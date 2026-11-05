using HARTPOS.DbModels;
using HARTPOS.Filters;
using HARTPOS.Service;
using HARTPOS.Models.Auth.DTOs;
using HARTPOS.Models.Auth.ViewModels;
using HARTPOS.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace HARTPOS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly ActivityLogRepository _activityLogRepo;

        public AccountController() : this(new UserRepository(), new ActivityLogRepository())
        {
        }

        public AccountController(UserRepository userRepo, ActivityLogRepository activityLogRepo)
        {
            _userRepo = userRepo;
            _activityLogRepo = activityLogRepo;
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

                    await _activityLogRepo.LogAsync(result.Username, "LOGIN", "User", result.UserID.ToString(), $"User {result.Username} logged in successfully", Request.UserHostAddress);

                    return Json(new AjaxRequestDTO
                    {
                        Status = true,
                        Message = "Login successful",
                        Data = new { redirectUrl = "/DashBoard/Dashboard" }
                    });
                }

                await _activityLogRepo.LogAsync(input.Username, "LOGIN_FAILED", "User", "", result.Message, Request.UserHostAddress);
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
                string logoutUsername = Session["Username"]?.ToString() ?? "Unknown";
                _activityLogRepo.LogAsync(logoutUsername, "LOGOUT", "User", "", $"User {logoutUsername} logged out", Request.UserHostAddress);

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

        [AuthorizePermission("Users_View", "Users_Manage")]
        public async Task<ActionResult> Users()
        {
            var users = await _userRepo.GetAllUsersAsync();
            return View(users);
        }

        [AuthorizePermission("Users_Manage")]
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
                if (user == null || user.UserID <= 0)
                {
                    return RedirectToAction("Users");
                }
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

        [AuthorizePermission("Users_Manage")]
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

                input.Username = (input.Username ?? "").Trim();
                input.FullName = (input.FullName ?? "").Trim();
                input.Role = string.Equals(input.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";

                if (string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.FullName))
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Username and full name are required." });
                }

                // For new user, password is required
                if (input.ID == 0 && string.IsNullOrEmpty(input.Password))
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Password is required for new user." });
                }

                if (!string.IsNullOrEmpty(input.Password) && input.Password.Length < 4)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Password must be at least 4 characters." });
                }

                var me = UserAccessService.Current(HttpContext);
                bool iAmAdmin = me != null ? me.IsAdmin : User.IsInRole("Admin");

                UserPermissionDto existing = null;
                if (input.ID > 0)
                {
                    existing = await _userRepo.GetUserByIDAsync(input.ID);
                    if (existing == null || existing.UserID <= 0)
                    {
                        return Json(new AjaxRequestDTO { Status = false, Message = "User not found. It may have been deleted." });
                    }
                    // Username can't be changed on edit.
                    input.Username = existing.Username;
                }
                else if (await _userRepo.UsernameExistsAsync(input.Username))
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "This username is already taken. Please choose another." });
                }

                // Only an Admin can create Admins or change an Admin account.
                bool touchesAdmin = input.Role == "Admin" || (existing != null && existing.Role == "Admin");
                if (touchesAdmin && !iAmAdmin)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Only an Admin can create or change Admin accounts." });
                }

                if (existing != null && me != null && existing.UserID == me.UserID && !input.IsActive)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "You can't deactivate your own account." });
                }

                // Never leave the system without an active Admin.
                bool losesAdmin = existing != null && existing.Role == "Admin" && existing.IsActive
                                  && (input.Role != "Admin" || !input.IsActive);
                if (losesAdmin && await _userRepo.CountOtherActiveAdminsAsync(existing.UserID) == 0)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "This is the last active Admin. Make another user Admin first." });
                }

                string loggedinUsername = Session["Username"]?.ToString() ?? "System";
                var result = await _userRepo.SaveUserAsync(input, loggedinUsername);
                UserAccessService.InvalidateAll();

                if (result > 0)
                {
                    string message = input.ID == 0 ? "User created successfully." : "User updated successfully.";
                    await _activityLogRepo.LogAsync(loggedinUsername, input.ID == 0 ? "USER_CREATE" : "USER_UPDATE", "User", result.ToString(), $"Username: {input.Username}, Role: {input.Role}", Request.UserHostAddress);
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

        [AuthorizePermission("Users_Manage")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteUser(int id)
        {
            try
            {
                var me = UserAccessService.Current(HttpContext);
                bool iAmAdmin = me != null ? me.IsAdmin : User.IsInRole("Admin");

                var target = await _userRepo.GetUserByIDAsync(id);
                if (target == null || target.UserID <= 0)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "User not found. It may already be deleted." });
                }
                if (me != null && target.UserID == me.UserID)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "You can't delete your own account." });
                }
                if (target.Role == "Admin")
                {
                    if (!iAmAdmin)
                    {
                        return Json(new AjaxRequestDTO { Status = false, Message = "Only an Admin can delete an Admin account." });
                    }
                    if (target.IsActive && await _userRepo.CountOtherActiveAdminsAsync(target.UserID) == 0)
                    {
                        return Json(new AjaxRequestDTO { Status = false, Message = "This is the last active Admin and can't be deleted." });
                    }
                }

                string loggedinUsername = Session["Username"]?.ToString() ?? "System";
                bool isDeleted = await _userRepo.DeleteUserAsync(id, loggedinUsername);
                UserAccessService.InvalidateAll();

                if (isDeleted)
                {
                    await _activityLogRepo.LogAsync(loggedinUsername, "USER_DELETE", "User", id.ToString(), $"User ID {id} deleted", Request.UserHostAddress);
                    return Json(new AjaxRequestDTO { Status = true, Message = "User deleted successfully." });
                }

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
            var access = UserAccessService.Current(HttpContext);
            bool hasPermission = access != null && access.IsActive && access.Has(permissionKey);
            return Json(new { HasPermission = hasPermission });
        }
    }
}
