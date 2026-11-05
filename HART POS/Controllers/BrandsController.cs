using HARTPOS.DbModels;
using HARTPOS.Filters;
using HARTPOS.Models;
using HARTPOS.Models.ManufacturerBrand;
using HARTPOS.Models.Menufacturer;
using HARTPOS.Repositories;
using HARTPOS.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HARTPOS.Controllers
{
    [Authorize]
    public class BrandsController : Controller
    {
        private readonly ManufacturerBrandsRepository _brandsRepository;

        public BrandsController() : this(new ManufacturerBrandsRepository())
        {
        }

        public BrandsController(ManufacturerBrandsRepository manufacturerBrandsRepository)
        {
            _brandsRepository = manufacturerBrandsRepository;
        }

        [AuthorizePermission("Brands_View")]
        public ActionResult Index()
        {
            // There is no Index view; the brands screen is "Brand".
            return RedirectToAction("Brand");
        }

        [AuthorizePermission("Brands_View", "Brands_Add", "Brands_Edit")]
        public ActionResult Brand(int? ID)
        {
            BrandViewModel model = new BrandViewModel()
            {
                ManufacturerID = ID,
            };
            return View(model);
        }

        [HttpPost]
        [AuthorizePermission("Brands_Add", "Brands_Edit")]
        public async Task<JsonResult> SaveBrand(SaveBrandInputDto formInput)
        {
            // A new record needs "Brands_Add"; changing an existing one needs "Brands_Edit".
            bool isNewRecord = (formInput.ID ?? 0) <= 0;
            if (!UserAccessService.CanSave(HttpContext, isNewRecord, "Brands_Add", "Brands_Edit"))
                return Json(new AjaxRequestDTO { Status = false, Message = UserAccessService.NoPermissionMessage(isNewRecord) });

            try
            { 
                var brand = new ManufacturerBrands
                {
                    ID = formInput.ID,
                    ManufacturerID = formInput.ManufacturerID,
                    Title = formInput.Title,
                };

                var isSaved = await _brandsRepository.SaveBrand(brand);

                if (isSaved)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Brand Saved Successfully" });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Brand due to technical error" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Brand due to technical error" });
            }
        }

        [HttpPost]
        [AuthorizePermission("Brands_View")]
        public async Task<JsonResult> GetPagedBrands(GetPagedBrandsInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _brandsRepository.GetPagedBrand(input);

                return Json(new PagedAjaxRequestOutputDTO
                {
                    draw = Request.Form.GetValues("draw").FirstOrDefault(),
                    recordsFiltered = data?.Item2 ?? string.Empty,
                    recordsTotal = data?.Item2 ?? string.Empty,
                    data = data?.Item1 ?? "[]"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new PagedAjaxRequestOutputDTO());
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetBrandShortList(GetPagedBrandsInputDto input)
        {
            try
            {
                var data = await _brandsRepository.GetBrandShortList(input);
                return Json(new AjaxRequestDTO
                {
                    Data = data,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new PagedAjaxRequestOutputDTO());
            }
        }
        [HttpPost]
        [AuthorizePermission("Brands_Delete")]
        public async Task<JsonResult> DeleteBrand(DeleteBrandInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var isDeleted = await _brandsRepository.DeleteBrand(input);
                if (isDeleted)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Brand Deleted successfully" });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Brand due to technical difficulties" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }
    }
}