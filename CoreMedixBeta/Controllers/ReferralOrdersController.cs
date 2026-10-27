using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.ReferralOrders.DTOs;
using CoreMedixBeta.Repositories;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class ReferralOrdersController : Controller
    {
        private readonly ReferralOrdersRepository _ordersRepo;

        public ReferralOrdersController() : this(new ReferralOrdersRepository())
        {
        }

        public ReferralOrdersController(ReferralOrdersRepository referralOrders)
        {
            _ordersRepo = referralOrders;
        }

        public ActionResult OrderList()
        {
            return View();
        }

        [HttpPost]
        [AuthorizePermission("ReferralOrders_View")]
        public async Task<JsonResult> GetPagedOrders(GetPagedOrdersInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _ordersRepo.GetPagedOrdersAsync(input);

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
    }
}