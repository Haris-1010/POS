using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Taxes;
using CoreMedixBeta.Models.Taxes.DTOs;
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
    public class TaxesController : Controller
    {
        private readonly TaxesRepository _taxesRepository;

        public TaxesController() : this(new TaxesRepository())
        {
        }

        public TaxesController(TaxesRepository manufacturerBrandsRepository)
        {
            _taxesRepository = manufacturerBrandsRepository;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Taxes()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermission("Taxes_Add", "Taxes_Edit")]
        public async Task<JsonResult> SaveTax(SaveTaxInputDto formInput)
        {
            try
            {  
                string operationType = formInput.ID > 0 ? "UPDATE" : "INSERT";
                Tax tax = new Tax
                {
                    ID = formInput.ID,
                    Title = formInput.Title,
                    PaymentMode = formInput.PaymentMode,
                    Percentage = formInput.Percentage,
                    LastModifiedBy = formInput.LastModifiedBy,
                };

                int Id = await _taxesRepository.SaveTaxesAsync(tax);
                if (Id <= 0)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Tax due to technical error" });
                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO { Status = true, Message = "Tax Saved Successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Tax due to technical error" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedTaxes(GetPagedTaxesInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _taxesRepository.GetPagedTaxesAsync(input);

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
        [AuthorizePermission("Taxes_Delete")]
        public async Task<JsonResult> DeleteTax(DeleteTaxInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                if (await _taxesRepository.DeleteTaxAsync(input))
                {
                    var now = ServerConfig.DateTimeNow;

                    return Json(new AjaxRequestDTO { Status = true, Message = "Tax Deleted successfully" });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Tax due to technical difficulties" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }
    }
}