using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Caching;

namespace HARTPOS.Service
{
    /// <summary>
    /// What a signed-in user is allowed to do, read from the database.
    /// </summary>
    public sealed class UserAccess
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();

        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

        public bool Has(string permissionKey) => IsAdmin || Permissions.Contains(permissionKey);

        public bool HasAny(params string[] permissionKeys) => IsAdmin || permissionKeys.Any(Permissions.Contains);
    }

    /// <summary>
    /// Single source of truth for role + permissions.
    ///
    /// Before this, permissions lived only in Session (filled once at login). That meant:
    ///  - when the session was lost (app pool recycle, IIS restart) the user stayed logged in
    ///    through the auth cookie but every page said "Access Denied" and the menu was empty;
    ///  - permission / role changes made by an admin only applied after the user logged out;
    ///  - a deactivated or deleted user kept working until their cookie expired.
    ///
    /// Now access is loaded from the DB, kept in memory for a few minutes, and dropped the
    /// moment an admin saves or deletes any user.
    /// </summary>
    public static class UserAccessService
    {
        private const string ItemsKey = "cm:user-access";
        private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);
        private static int _version;

        private static string CacheKey(string username) =>
            "cm:access:" + Volatile.Read(ref _version) + ":" + username.Trim().ToLowerInvariant();

        /// <summary>Clears cached access for every user (call after any user/permission change).</summary>
        public static void InvalidateAll() => Interlocked.Increment(ref _version);

        /// <summary>
        /// Returns the user's access, or null when the user no longer exists / is deleted.
        /// Throws if the database can't be reached so callers can fall back safely.
        /// </summary>
        public static UserAccess Load(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string key = CacheKey(username);
            if (HttpRuntime.Cache[key] is UserAccess cached)
                return cached;

            var dbValues = new Dictionary<string, string> { { "@Username", username } };

            var user = DBHelper.Get<HARTPOS.Models.Auth.Users>(
                @"SELECT ID, Username, FullName, Role, IsActive
                  FROM Auth.Users
                  WHERE Username = @Username AND IsDeleted = 0", Params: dbValues);

            if (user == null || user.ID <= 0)
                return null;

            var permissions = DBHelper.QueryColumn(
                @"SELECT P.PermissionKey
                  FROM Auth.UserPermissions UP
                  INNER JOIN Auth.Permissions P ON UP.PermissionID = P.ID
                  WHERE UP.UserID = @UserID",
                new Dictionary<string, string> { { "@UserID", user.ID.ToString() } });

            var access = new UserAccess
            {
                UserID = user.ID,
                Username = user.Username,
                FullName = user.FullName,
                Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role,
                IsActive = user.IsActive,
                Permissions = permissions ?? new List<string>()
            };

            HttpRuntime.Cache.Insert(key, access, null, DateTime.UtcNow.Add(CacheFor),
                Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);

            return access;
        }

        /// <summary>Access for the current request (set in Global.asax), or null if anonymous.</summary>
        public static UserAccess Current(HttpContextBase context)
        {
            if (context == null)
                return null;

            if (context.Items[ItemsKey] is UserAccess fromItems)
                return fromItems;

            if (context.User == null || !context.User.Identity.IsAuthenticated)
                return null;

            try
            {
                var access = Load(context.User.Identity.Name);
                if (access != null)
                    context.Items[ItemsKey] = access;
                return access;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ACCESS] Load failed: " + ex.Message);
                return null;
            }
        }

        public static UserAccess Current() =>
            HttpContext.Current == null ? null : Current(new HttpContextWrapper(HttpContext.Current));

        internal static void SetCurrent(HttpContext context, UserAccess access)
        {
            if (context != null && access != null)
                context.Items[ItemsKey] = access;
        }

        /// <summary>
        /// Save actions accept "Add" or "Edit" at the door; this decides by what is actually
        /// happening: a new record needs addKey, changing an existing one needs editKey.
        /// </summary>
        public static bool CanSave(HttpContextBase context, bool isNew, string addKey, string editKey)
        {
            var access = Current(context);
            if (access == null)
                return context?.User != null && context.User.IsInRole("Admin");
            return access.Has(isNew ? addKey : editKey);
        }

        public static bool Can(HttpContextBase context, string key)
        {
            var access = Current(context);
            if (access == null)
                return context?.User != null && context.User.IsInRole("Admin");
            return access.Has(key);
        }

        public static string NoPermissionMessage(bool isNew) =>
            isNew ? "You don't have permission to add this." : "You don't have permission to edit this.";

        /// <summary>Copies access into Session so older code reading Session[...] stays correct.</summary>
        public static void SyncSession(HttpSessionStateBase session, UserAccess access)
        {
            if (session == null || access == null)
                return;

            session["UserID"] = access.UserID;
            session["Username"] = access.Username;
            session["FullName"] = access.FullName;
            session["Role"] = access.Role;
            session["Permissions"] = access.Permissions;
            if (session["LoginTime"] == null)
                session["LoginTime"] = DateTime.Now;
        }
    }
}
