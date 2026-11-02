using System.Web;
using System.Web.Mvc;
using HARTPOS.Filters;

namespace HARTPOS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LicenseEnforcementFilter());
        }
    }
}
