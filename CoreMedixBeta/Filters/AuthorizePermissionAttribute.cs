using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Filters
{
    /// <summary>
    /// Custom authorization attribute that checks granular permissions from session.
    /// Usage: [AuthorizePermission("Patient_Add")] or [AuthorizePermission("Patient_Add", "Patient_Edit")]
    /// Admin role bypasses all permission checks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AuthorizePermissionAttribute : AuthorizeAttribute
    {
        private readonly string[] _permissionKeys;

        public AuthorizePermissionAttribute(params string[] permissionKeys)
        {
            _permissionKeys = permissionKeys;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.User == null)
                return false;

            // Must be authenticated first
            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            // Admin role bypasses all permission checks
            if (httpContext.User.IsInRole("Admin"))
                return true;

            // Get permissions from session
            var permissions = httpContext.Session["Permissions"] as List<string>;
            if (permissions == null)
                return false;

            // User must have at least ONE of the specified permissions
            return _permissionKeys.Any(key => permissions.Contains(key));
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
        {
                // User is logged in but doesn't have permission
                if (filterContext.RequestContext.HttpContext.Request.IsAjaxRequest())
                {
                    // For AJAX requests, return JSON error
                    filterContext.Result = new JsonResult
                    {
                        Data = new
                        {
                            Status = false,
                            Message = "You don't have permission to perform this action."
                        },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    // For normal requests, redirect to Access Denied
                    filterContext.Result = new RedirectResult("/DashBoard/AccessDenied");
                }
            }
            else
            {
                // Not logged in, redirect to login
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }

    /// <summary>
    /// Extension method to check if request is AJAX
    /// </summary>
    public static class HttpRequestExtensions
    {
        public static bool IsAjaxRequest(this HttpRequestBase request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request["X-Requested-With"] == "XMLHttpRequest")
                return true;

            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return true;

            return false;
        }
    }
}
