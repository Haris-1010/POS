using CoreMedixBeta.Models.Auth;
using CoreMedixBeta.Models.Auth.DTOs;
using CoreMedixBeta.Models.Auth.ViewModels;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CoreMedixBeta.Repositories
{
    public class UserRepository
    {
        /// <summary>
        /// Hash password using SHA256
        /// </summary>
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("X2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Authenticate user login
        /// </summary>
        public async Task<LoginOutputDto> LoginAsync(LoginInputDto input)
        {
            try
            {
                string passwordHash = HashPassword(input.Password);
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Username={input.Username}, Hash={passwordHash}");

                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@Username", input.Username },
                    { "@PasswordHash", passwordHash }
                };

                string query = @"SELECT ID, Username, FullName, Role, IsActive
                                 FROM Auth.Users
                                 WHERE Username = @Username AND PasswordHash = @PasswordHash AND IsDeleted = 0";

                System.Diagnostics.Debug.WriteLine($"[LOGIN] Query: {query}");
                var user = await DBHelper.GetAsync<Users>(query, Params: dbValues);

                if (user != null && user.ID > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] User found: ID={user.ID}, Role={user.Role}, IsActive={user.IsActive}");

                    if (!user.IsActive)
                    {
                        return new LoginOutputDto
                        {
                            IsValid = false,
                            Message = "Your account is deactivated. Please contact administrator."
                        };
                    }

                    return new LoginOutputDto
                    {
                        IsValid = true,
                        UserID = user.ID,
                        Username = user.Username,
                        FullName = user.FullName,
                        Role = user.Role,
                        Message = "Login successful"
                    };
                }

                System.Diagnostics.Debug.WriteLine("[LOGIN] No user found with matching credentials");
                return new LoginOutputDto
                {
                    IsValid = false,
                    Message = "Invalid username or password."
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Exception: {ex.Message}\n{ex.StackTrace}");
                return new LoginOutputDto
                {
                    IsValid = false,
                    Message = "Technical error: " + ex.Message
                };
            }
        }

        /// <summary>
        /// Get all active users
        /// </summary>
        public async Task<List<UserListDto>> GetAllUsersAsync()
        {
            try
            {
                string query = @"SELECT ID, Username, FullName, Email, Phone, Role, IsActive, CreatedOn
                                 FROM Auth.Users
                                 WHERE IsDeleted = 0
                                 ORDER BY CreatedOn DESC";

                return await DBHelper.GetListAsync<UserListDto>(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<UserListDto>();
            }
        }

        /// <summary>
        /// Get user by ID with permissions
        /// </summary>
        public async Task<UserPermissionDto> GetUserByIDAsync(int userID)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", userID.ToString() }
                };

                string query = @"SELECT ID AS UserID, Username, FullName, Role, Email, Phone, IsActive
                                 FROM Auth.Users
                                 WHERE ID = @ID AND IsDeleted = 0";

                return await DBHelper.GetAsync<UserPermissionDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new UserPermissionDto();
            }
        }

        /// <summary>
        /// Get user's assigned permission IDs
        /// </summary>
        public async Task<List<int>> GetUserPermissionIDsAsync(int userID)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@UserID", userID.ToString() }
                };

                string query = @"SELECT PermissionID FROM Auth.UserPermissions WHERE UserID = @UserID";

                var result = await DBHelper.GetListAsync<UserPermissions>(query, Params: dbValues);
                return result.Select(x => x.PermissionID).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<int>();
            }
        }

        /// <summary>
        /// Get user's permission keys (for session storage)
        /// </summary>
        public async Task<List<string>> GetUserPermissionKeysAsync(int userID)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@UserID", userID.ToString() }
                };

                string query = @"SELECT P.PermissionKey
                                 FROM Auth.UserPermissions UP
                                 INNER JOIN Auth.Permissions P ON UP.PermissionID = P.ID
                                 WHERE UP.UserID = @UserID";

                return await DBHelper.QueryColumnAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// Get all available permissions
        /// </summary>
        public async Task<List<Permissions>> GetAllPermissionsAsync()
        {
            try
            {
                string query = @"SELECT ID, PermissionKey, PermissionName, Module
                                 FROM Auth.Permissions
                                 ORDER BY Module, PermissionKey";

                return await DBHelper.GetListAsync<Permissions>(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<Permissions>();
            }
        }

        /// <summary>
        /// Create or Update user with permissions
        /// </summary>
        public async Task<int> SaveUserAsync(ManageUserInputDto input, string loggedinUsername)
        {
            try
            {
                if (input.ID == 0)
                {
                    // CREATE NEW USER
                    string passwordHash = HashPassword(input.Password);

                    Dictionary<string, string> dbValues = new Dictionary<string, string>
                    {
                        { "@Username", input.Username },
                        { "@PasswordHash", passwordHash },
                        { "@FullName", input.FullName },
                        { "@Email", input.Email ?? "" },
                        { "@Phone", input.Phone ?? "" },
                        { "@Role", input.Role },
                        { "@IsActive", input.IsActive ? "1" : "0" },
                        { "@CreatedBy", loggedinUsername }
                    };

                    string query = @"INSERT INTO Auth.Users (Username, PasswordHash, FullName, Email, Phone, Role, IsActive, CreatedBy)
                                     VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @Role, @IsActive, @CreatedBy);
                                     SELECT SCOPE_IDENTITY();";

                    var result = await DBHelper.GetSingleColumnAsync(query, dbValues);

                    if (int.TryParse(result, out int newID))
                    {
                        // Assign permissions
                        await AssignPermissionsAsync(newID, input.PermissionIDs);
                        return newID;
                    }
                    return 0;
                }
                else
                {
                    // UPDATE EXISTING USER
                    Dictionary<string, string> updateValues = new Dictionary<string, string>
                    {
                        { "@FullName", input.FullName },
                        { "@Email", input.Email ?? "" },
                        { "@Phone", input.Phone ?? "" },
                        { "@Role", input.Role },
                        { "@IsActive", input.IsActive ? "1" : "0" },
                        { "@LastModifiedBy", loggedinUsername },
                        { "@ID", input.ID.ToString() }
                    };

                    // If password is provided, update it too
                    string passwordUpdate = "";
                    if (!string.IsNullOrEmpty(input.Password))
                    {
                        string passwordHash = HashPassword(input.Password);
                        updateValues.Add("@PasswordHash", passwordHash);
                        passwordUpdate = ", PasswordHash = @PasswordHash";
                    }

                    string query = $@"UPDATE Auth.Users
                                      SET FullName = @FullName, Email = @Email, Phone = @Phone,
                                          Role = @Role, IsActive = @IsActive, LastModifiedOn = GETDATE(),
                                          LastModifiedBy = @LastModifiedBy{passwordUpdate}
                                      WHERE ID = @ID";

                    bool isUpdated = await DBHelper.ExecuteQueryAsync(query, updateValues);

                    // Re-assign permissions
                    await AssignPermissionsAsync(input.ID, input.PermissionIDs);

                    return input.ID;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Assign permissions to a user (replaces existing)
        /// </summary>
        public async Task<bool> AssignPermissionsAsync(int userID, List<int> permissionIDs)
        {
            try
            {
                // First, remove existing permissions
                Dictionary<string, string> deleteValues = new Dictionary<string, string>
                {
                    { "@UserID", userID.ToString() }
                };

                string deleteQuery = @"DELETE FROM Auth.UserPermissions WHERE UserID = @UserID";
                await DBHelper.ExecuteQueryAsync(deleteQuery, deleteValues);

                // Then, insert new permissions
                if (permissionIDs != null && permissionIDs.Count > 0)
                {
                    foreach (int permissionID in permissionIDs)
                    {
                        Dictionary<string, string> insertValues = new Dictionary<string, string>
                        {
                            { "@UserID", userID.ToString() },
                            { "@PermissionID", permissionID.ToString() }
                        };

                        string insertQuery = @"INSERT INTO Auth.UserPermissions (UserID, PermissionID)
                                               VALUES (@UserID, @PermissionID)";
                        await DBHelper.ExecuteQueryAsync(insertQuery, insertValues);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Soft delete user
        /// </summary>
        public async Task<bool> DeleteUserAsync(int userID, string deletedBy)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", userID.ToString() },
                    { "@DeletedBy", deletedBy },
                    { "@DeletedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                string query = @"UPDATE Auth.Users
                                 SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn, IsActive = 0
                                 WHERE ID = @ID";

                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get permissions grouped by module for ManageUser view
        /// </summary>
        public async Task<List<PermissionGroupVM>> GetPermissionsGroupedByModuleAsync(List<int> selectedPermissionIDs = null)
        {
            try
            {
                var allPermissions = await GetAllPermissionsAsync();
                selectedPermissionIDs = selectedPermissionIDs ?? new List<int>();

                var grouped = allPermissions
                    .GroupBy(p => p.Module)
                    .Select(g => new PermissionGroupVM
                    {
                        Module = g.Key,
                        Permissions = g.Select(p => new PermissionItemVM
                        {
                            ID = p.ID,
                            PermissionKey = p.PermissionKey,
                            PermissionName = p.PermissionName,
                            IsSelected = selectedPermissionIDs.Contains(p.ID)
                        }).ToList()
                    })
                    .ToList();

                return grouped;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<PermissionGroupVM>();
            }
        }
    }
}
