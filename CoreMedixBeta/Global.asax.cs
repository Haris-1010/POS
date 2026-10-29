using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using CoreMedixBeta.Service;

namespace CoreMedixBeta
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

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

                // Read role from ticket userData
                string role = ticket.UserData;
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
    }
}
