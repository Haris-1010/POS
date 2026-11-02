using HARTPOS.Service;
using System;
using System.IO;
using System.Security;
using System.Web;
using System.Web.Mvc;

namespace HARTPOS.Controllers
{
    [AllowAnonymous]
    public sealed class LicenseController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.LicenseNotice = TempData["LicenseNotice"] as string;
            ViewBag.LicenseInstallSucceeded = TempData["LicenseInstallSucceeded"] as bool? == true;
            return View(LicenseService.GetStatus());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Install(HttpPostedFileBase licenseFile)
        {
            if (licenseFile == null || licenseFile.ContentLength <= 0)
            {
                TempData["LicenseNotice"] = "Choose a non-empty signed license file.";
                TempData["LicenseInstallSucceeded"] = false;
                return RedirectToAction("Index");
            }

            LicenseStatus result;
            try
            {
                result = LicenseService.InstallLicense(licenseFile.InputStream);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                      ex is SecurityException || ex is ArgumentException ||
                                      ex is NotSupportedException)
            {
                System.Diagnostics.Trace.TraceError("POS license installation failed: " + ex);
                TempData["LicenseNotice"] = "IIS could not access the license folder. Grant the application pool access, then try again.";
                TempData["LicenseInstallSucceeded"] = false;
                return RedirectToAction("Index");
            }

            TempData["LicenseNotice"] = result.IsValid
                ? "The signed license was installed successfully."
                : result.Message;
            TempData["LicenseInstallSucceeded"] = result.IsValid;

            return RedirectToAction("Index");
        }
    }
}
