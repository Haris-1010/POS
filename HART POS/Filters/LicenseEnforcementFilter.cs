using HARTPOS.Service;
using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace HARTPOS.Filters
{
    public sealed class LicenseEnforcementFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            string controller = filterContext.RouteData.Values["controller"] as string;
            if (string.Equals(controller, "License", StringComparison.OrdinalIgnoreCase))
                return;

            LicenseStatus status = LicenseService.GetStatus();
            if (status.IsValid)
                return;

            string activationUrl = new UrlHelper(filterContext.RequestContext)
                .Action("Index", "License");
            filterContext.Result = new RedirectResult(activationUrl);
        }
    }
}
