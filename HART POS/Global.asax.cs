using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using HARTPOS.Service;

namespace HARTPOS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Adds any permissions the code uses but the database doesn't have yet.
            PermissionCatalog.EnsureSeeded();
            // Small idempotent DB changes (e.g. Manufacturer/Type optional on items).
            SchemaUpdates.Run();

            // Starts the in-process database backup scheduler. The persistent
            // Windows Scheduled Task (Scripts/Setup-BackupScheduledTask.ps1)
            // remains the fallback when IIS is stopped or the app pool recycles.
            BackupSchedulerService.Start();
        }

        protected void Application_End()
        {
            BackupSchedulerService.Stop();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            try
            {
                HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie == null || string.IsNullOrEmpty(authCookie.Value))
                    return;

                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null || ticket.Expired)
                    return;

                // Role comes from the database (cached), so an admin's changes apply right away.
                // Falls back to the role stored in the ticket only if the DB can't be reached.
                string role = ticket.UserData;
                try
                {
                    UserAccess access = UserAccessService.Load(ticket.Name);
                    if (access == null || !access.IsActive)
                    {
                        // Deleted or deactivated: treat as signed out (the forms module has
                        // already set an authenticated user from the cookie, so replace it).
                        HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(
                            new System.Security.Principal.GenericIdentity(string.Empty), new string[0]);
                        return;
                    }
                    role = access.Role;
                    UserAccessService.SetCurrent(HttpContext.Current, access);
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine("[ACCESS] Using ticket role, DB unavailable: " + dbEx.Message);
                }

                string[] roles = string.IsNullOrEmpty(role) ? new string[] { } : new string[] { role };

                // Set HttpContext.User with role
                FormsIdentity identity = new FormsIdentity(ticket);
                System.Security.Principal.GenericPrincipal principal = new System.Security.Principal.GenericPrincipal(identity, roles);
                HttpContext.Current.User = principal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Application_AuthenticateRequest error: " + ex.Message);
            }
        }

        // Keeps Session in step with the database so code that reads Session["Permissions"],
        // Session["Username"] etc. sees current values, even after the session was lost.
        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            if (context?.Session == null || context.User == null || !context.User.Identity.IsAuthenticated)
                return;

            UserAccess access = UserAccessService.Current(new HttpContextWrapper(context));
            if (access != null)
                UserAccessService.SyncSession(new HttpSessionStateWrapper(context.Session), access);
        }
    }
}
