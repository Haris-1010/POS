using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Auth.DTOs;
using CoreMedixBeta.Models.Auth.ViewModels;
using CoreMedixBeta.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CoreMedixBeta.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepo;

        public AccountController() : this(new UserRepository())
        {
        }

        public AccountController(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // ==========================================
        // LOGIN
        // ==========================================
        [AllowAnonymous]
        public ActionResult Login(string returnUrl = "")
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "DashBoard");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Login(LoginInputDto input)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Attempt: Username={input?.Username}");

                if (string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.Password))
                {
                    System.Diagnostics.Debug.WriteLine("[LOGIN] Failed: Username or password is empty");
                    return Json(new AjaxRequestDTO { Status = false, Message = "Username and password are required." });
                }

                var result = await _userRepo.LoginAsync(input);

                System.Diagnostics.Debug.WriteLine($"[LOGIN] Result: IsValid={result.IsValid}, Message={result.Message}");

                if (result.IsValid)
                {
                    // Create FormsAuthentication ticket WITH role in user data
                    string userData = result.Role; // Store role in ticket
                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                        1,                          // version
                        result.Username,            // username
                        DateTime.Now,               // issue date
                        DateTime.Now.AddMinutes(480), // expiration (8 hours)
                        false,                      // persistent
                        userData,                   // user data (role)
                        FormsAuthentication.FormsCookiePath
                    );
                    string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    authCookie.HttpOnly = true;
                    Response.Cookies.Add(authCookie);

                    // Set the user principal with role
                    string[] roles = new string[] { result.Role };
                    FormsIdentity identity = new FormsIdentity(ticket);
                    System.Security.Principal.GenericPrincipal principal = new System.Security.Principal.GenericPrincipal(identity, roles);
                    HttpContext.User = principal;

                    // Store user info in session
                    Session["UserID"] = result.UserID;
                    Session["Username"] = result.Username;
                    Session["FullName"] = result.FullName;
                    Session["Role"] = result.Role;
                    Session["LoginTime"] = DateTime.Now;

                    // Store permissions in session
                    var permissions = await _userRepo.GetUserPermissionKeysAsync(result.UserID);
                    Session["Permissions"] = permissions;

                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Success: UserID={result.UserID}, Role={result.Role}");

                    return Json(new AjaxRequestDTO
                    {
                        Status = true,
                        Message = "Login successful",
                        Data = new { redirectUrl = "/DashBoard/Dashboard" }
                    });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = result.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Exception: {ex.Message}\n{ex.StackTrace}");
                return Json(new AjaxRequestDTO { Status = false, Message = "Technical error: " + ex.Message });
            }
        }

        // ==========================================
        // LOGOUT
        // ==========================================
        [HttpPost]
        public JsonResult LogOut()
        {
            try
            {
                // Clear session
                Session.Abandon();
                Session.Clear();

                // Remove FormsAuthentication cookie
                FormsAuthentication.SignOut();

                // Remove auth cookie
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
                    authCookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(authCookie);
                }

                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Message = "You have been logged out successfully.",
                    Data = new { redirectUrl = "/Account/Login" }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Error during logout." });
            }
        }

        // ==========================================
        // USER MANAGEMENT (Admin Only)
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Users()
        {
            var users = await _userRepo.GetAllUsersAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [OutputCache(Duration = 0, Location = System.Web.UI.OutputCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> ManageUser(int? id)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            var vm = new ManageUserVM();

            if (id.HasValue && id.Value > 0)
            {
                // EDIT MODE
                var user = await _userRepo.GetUserByIDAsync(id.Value);
                var permissionIDs = await _userRepo.GetUserPermissionIDsAsync(id.Value);

                vm.ID = user.UserID;
                vm.Username = user.Username;
                vm.FullName = user.FullName;
                vm.Email = user.Email;
                vm.Phone = user.Phone;
                vm.Role = user.Role;
                vm.IsActive = user.IsActive;
                vm.IsNewUser = false;
                vm.SelectedPermissionIDs = permissionIDs;
            }
            else
            {
                // CREATE MODE
                vm.IsNewUser = true;
            }

            // Get permissions grouped by module
            vm.PermissionGroups = await _userRepo.GetPermissionsGroupedByModuleAsync(vm.SelectedPermissionIDs);

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveUser(ManageUserInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Please fill all required fields." });
                }

                // For new user, password is required
                if (input.ID == 0 && string.IsNullOrEmpty(input.Password))
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Password is required for new user." });
                }

                string loggedinUsername = Session["Username"]?.ToString() ?? "System";
                var result = await _userRepo.SaveUserAsync(input, loggedinUsername);

                if (result > 0)
                {
                    string message = input.ID == 0 ? "User created successfully." : "User updated successfully.";
                    return Json(new AjaxRequestDTO { Status = true, Message = message });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to save user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Technical error: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteUser(int id)
        {
            try
            {
                string loggedinUsername = Session["Username"]?.ToString() ?? "System";
                bool isDeleted = await _userRepo.DeleteUserAsync(id, loggedinUsername);

                if (isDeleted)
                    return Json(new AjaxRequestDTO { Status = true, Message = "User deleted successfully." });

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to delete user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Technical error: " + ex.Message });
            }
        }

        // ==========================================
        // CHECK PERMISSION (AJAX Helper)
        // ==========================================
        [HttpPost]
        public JsonResult HasPermission(string permissionKey)
        {
            var permissions = Session["Permissions"] as List<string>;
            bool hasPermission = permissions != null && permissions.Contains(permissionKey);
            return Json(new { HasPermission = hasPermission });
        }
    }
}
