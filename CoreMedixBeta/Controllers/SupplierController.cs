using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Patient.DTOs;
using CoreMedixBeta.Models.Supplier;
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
    public class SupplierController : Controller
    {
        private readonly SupplierRepository _supplierRepository;
        private readonly PatientRepository _patientRepository;

        public SupplierController() : this(new SupplierRepository(), new PatientRepository())
        {
        }

        public SupplierController(SupplierRepository supplierRepository, PatientRepository patientRepository)
        {
            _supplierRepository = supplierRepository;
            _patientRepository = patientRepository;
        }

        public async Task<ActionResult> Suppliers()
        {
            return View(new SuppliersViewModel
            {
                Cities = await _patientRepository.GetAllCitiesNamesAsync()
            });
        }

        [HttpPost]
        [AuthorizePermission("Suppliers_Add", "Suppliers_Edit")]
        public async Task<JsonResult> SaveSupplier(Supplier formInput)
        {
            try
            {
                string currentUsername = Session["Username"]?.ToString() ?? "System";
                formInput.CreatedBy = currentUsername;
                formInput.LastModifiedBy = currentUsername;

                var customerId = await _supplierRepository.SaveSupplierAsync(formInput);

                if (customerId > 0)
                {
                    return Json(new
                    {
                        Status = true,
                        Message = "Supplier saved successfully",
                        Data = customerId
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    Status = false,
                    Message = "Failed to save Supplier due to technical error"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return Json(new
                {
                    Status = false,
                    Message = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [AuthorizePermission("Suppliers_View")]
        public async Task<JsonResult> GetPagedSuppliers(GetPagedSuppliersInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _supplierRepository.GetPagedSuppliersAsync(input);

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
        [AuthorizePermission("Suppliers_Delete")]
        public async Task<JsonResult> DeleteSupplier(DeleteSupplierInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";

                if (await _supplierRepository.DeleteSupplierAsync(input))
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Supplier Deleted successfully", });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Supplier due to technical difficulties" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        //[HttpPost]
        //public async Task<JsonResult> GetSuppliersShortList(RequestByMasterUsernameDTO input)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //            return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

        //        var data = await _supplierRepository.GetSupplierShortList(new GetSupplierShortListInputDto
        //        {
        //            MasterUsername = input.MasterUsername
        //        });

        //        if (data != null && data.Count > 0)
        //        {
        //            return Json(new AjaxRequestDTO { Status = true, Data = data, Message = "Suppliers Fetched successfully" });
        //        }

        //        return Json(new AjaxRequestDTO { Status = false, Message = "Data Not Found" });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return Json(new AjaxRequestDTO { Status = false, Message = ex.Message });
        //    }
        //}
    }
}