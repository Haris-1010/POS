using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models.Branding;
using CoreMedixBeta.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class BrandingController : Controller
    {
        private readonly BrandingRepository _branding;

        public BrandingController() : this(new BrandingRepository())
        {
        }

        public BrandingController(BrandingRepository brandingRepository)
        {
            _branding = brandingRepository;
        }

        [AuthorizePermission("Branding_View")]
        public ActionResult Settings()
        {
            string location = ServerConfig.GetLocationIDCallback?.Invoke() ?? "";

            ViewData["branding"] = BrandingRepository.get(location);
            return View();
        }

        [HttpPost]
        [AuthorizePermission("Branding_Manage")]
        public JsonResult Update(string branding, HttpPostedFileBase logo, HttpPostedFileBase headerImage, HttpPostedFileBase WaterMarkImage)
        {
            string username = Session["Username"]?.ToString() ?? "System";
            string location = ServerConfig.GetLocationIDCallback?.Invoke() ?? "";
            BrandingRepository details = JsonConvert.DeserializeObject<BrandingRepository>(branding);
            var brandingSource = !string.IsNullOrEmpty(_branding.BrandingSource) ? _branding.BrandingSource : details.BrandingSource;

            details.BrandingSource = "Primary";
            details.Type = "Primary";

            if (logo != null && logo.ContentLength > 0)
            {
                var fileName = Path.GetFileName(logo.FileName);
                string fileExtension = fileName.Remove(0, fileName.IndexOf('.') + 1);
                fileName = username + "_logo." + fileExtension;
                fileName = Regex.Replace(fileName, @"\s", "");
                string folder_name = BlobUploader.GetFolderName(username);
                folder_name = Regex.Replace(folder_name, @"\s", "");
                string SaveLocation = ServerConfig.BlobPath + folder_name + "/" + fileName;
                SaveLocation = Regex.Replace(SaveLocation, @"\s", "");
                details.logo = SaveLocation;
                BlobUploader.UploadTo(folder_name, fileName, logo.InputStream);
            }

            if (headerImage != null && headerImage.ContentLength > 0)
            {
                var fileName = Path.GetFileName(headerImage.FileName);
                string fileExtension = fileName.Remove(0, fileName.IndexOf('.') + 1);
                fileName = username + "_headerImage." + fileExtension;
                string folder_name = BlobUploader.GetFolderName(username);
                string SaveLocation = ServerConfig.BlobPath + folder_name + "/" + fileName;
                details.headerImage = SaveLocation;
                BlobUploader.UploadTo(folder_name, fileName, headerImage.InputStream);
            }

            if (WaterMarkImage != null && WaterMarkImage.ContentLength > 0)
            {
                var fileName = Path.GetFileName(WaterMarkImage.FileName);
                string fileExtension = fileName.Remove(0, fileName.IndexOf('.') + 1);
                fileName = username + "_waterMarkImage." + fileExtension;
                string folder_name = BlobUploader.GetFolderName(username);
                string SaveLocation = ServerConfig.BlobPath + folder_name + "/" + fileName;
                details.WaterMarkImage = SaveLocation;
                BlobUploader.UploadTo(folder_name, fileName, WaterMarkImage.InputStream);
            }
            bool st;

            // Wants To Update Primary branding
            if (details.Type == "Primary")
            {
                st = details.update_branding(location, "Primary", username, brandingSource);
            }
            //Wants to Update Personal Branding
            else
            {
                st = details.update_branding(location, "Secondary", username, brandingSource);
            }

            if (st)
            {
                return Json(new JsonResp(true, "Information has been updated successfully."), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new JsonResp(false, "Information has not been Updated. Please try again."), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult LoadMultipleBranding()
        {
            try
            {
                string username = Session["Username"]?.ToString() ?? "System";
                string location = ServerConfig.GetLocationIDCallback?.Invoke() ?? "";
                var result = BrandingShortVM.GetBrandings(location, username);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult UpdateBrandingSource(string Source)
        {
            try
            {
                string username = Session["Username"]?.ToString() ?? "System";
                string location = ServerConfig.GetLocationIDCallback?.Invoke() ?? "";
                var result = _branding.UpdateBrandingSource(username, location, Source);
                if (result)
                {
                    var branding = _branding.GetBranding(location, username, Source);
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
    }
}