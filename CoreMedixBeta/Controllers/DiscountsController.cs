using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Discounts;
using CoreMedixBeta.Models.Discounts.DTOs;
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
    public class DiscountsController : Controller
    {
        private readonly DiscountsRepository _discountsRepository;

        public DiscountsController() : this(new DiscountsRepository())
        {
        }

        public DiscountsController(DiscountsRepository discountsRepository)
        {
            _discountsRepository = discountsRepository;
        }

        public ActionResult Discounts()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermission("Discounts_Add", "Discounts_Edit")]
        public async Task<JsonResult> SaveDiscount(SaveDiscountInputDto formInput)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                string operationType = formInput.ID > 0 ? "UPDATE" : "INSERT";
                Discount discount = new Discount
                {
                    ID = formInput.ID,
                    DiscountName = formInput.DiscountName,
                    DiscountType = formInput.DiscountType,
                    DiscountValue = formInput.DiscountValue,
                    IsActive = formInput.IsActive,
                    TimeStamp = DateTime.Now,
                };
                int Id = await _discountsRepository.SaveDiscountAsync(discount);
                if (Id <= 0) return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Discount due to technical error" });

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO { Status = true, Message = "Discount Saved Successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Patient due to technical error" });
            }
        }

        [HttpPost] 
        public async Task<JsonResult> GetPagedDiscounts()
        {
            try
            {
                var data = await _discountsRepository.GetPagedDiscountsAsync(new PagedRequestInputDto
                {
                    DataTableValues = Common.GetMappedDataTableValues(HttpContext)
                });

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
        [AuthorizePermission("Discounts_Delete")]
        public async Task<JsonResult> DeleteDiscount(DeleteDiscountInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                if (await _discountsRepository.DeleteDiscountAsync(input))
                {
                    var now = ServerConfig.DateTimeNow;

                    return Json(new AjaxRequestDTO { Status = true, Message = "Customer Deleted successfully", });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Customer due to technical difficulties" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetDiscountsShort()
        {
            try
            {
                var resp = await _discountsRepository.GetDiscountsShortListAsync(new RequestByMasterUsernameDTO
                {
                });

                return Json(new AjaxRequestDTO { Data = resp, Status = true, Message = "Success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }
    }
}