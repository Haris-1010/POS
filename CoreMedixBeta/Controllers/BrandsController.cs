using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.ManufacturerBrand;
using CoreMedixBeta.Models.Menufacturer;
using CoreMedixBeta.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
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

        public ActionResult Index()
        {
            return View();
        }

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