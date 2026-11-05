using HARTPOS.DbModels;
using HARTPOS.Filters;
using HARTPOS.Models;
using HARTPOS.Models.Patient;
using HARTPOS.Models.Patient.DTOs;
using HARTPOS.Repositories;
using InstaCareEMR.Core.DBHelpers;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
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
    public class PatientController : Controller
    {
        private readonly PatientRepository _patientRepository;
        private readonly ActivityLogRepository _activityLogRepo = new ActivityLogRepository();

        public PatientController() : this(new PatientRepository())
        {
        }

        public PatientController(PatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        [AuthorizePermission("Patient_View")]
        public async Task<ActionResult> Patients()
        {
            return View(new PatientVM
            {
                Cities = await _patientRepository.GetAllCitiesNamesAsync(),
                Patient = new Patients()   // ensures Patient is never null
            });
        }


        [AuthorizePermission("Patient_Add", "Patient_Edit")]
        public async Task<JsonResult> SavePatients(Patients formInput)
        {
            // A new record needs "Patient_Add"; changing an existing one needs "Patient_Edit".
            bool isNewRecord = formInput.ID <= 0;
            if (!UserAccessService.CanSave(HttpContext, isNewRecord, "Patient_Add", "Patient_Edit"))
                return Json(new AjaxRequestDTO { Status = false, Message = UserAccessService.NoPermissionMessage(isNewRecord) });

            try
            {
                if (string.IsNullOrWhiteSpace(formInput?.Name))
                {
                    return Json(new
                    {
                        Status = false,
                        Message = "Customer name is required"
                    }, JsonRequestBehavior.AllowGet);
                }

                string currentUsername = Session["Username"]?.ToString() ?? "System";
                formInput.CreatedBy = currentUsername;
                formInput.LastModifiedBy = currentUsername;

                var customerId = await _patientRepository.SavePatientsAsync(formInput);

                if (customerId > 0)
                {
                    await _activityLogRepo.LogAsync(currentUsername, formInput.ID == 0 ? "CUSTOMER_CREATE" : "CUSTOMER_UPDATE", "Customer", customerId.ToString(), $"Customer {formInput.Name} saved", Request.UserHostAddress);
                    return Json(new
                    {
                        Status = true,
                        Message = "Customer saved successfully",
                        Data = customerId
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    Status = false,
                    Message = "Failed to save customer due to technical error"
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
        [AuthorizePermission("Patient_View", "Sales_View", "Sales_Add", "Sales_Edit")]
        public async Task<JsonResult> GetAllCustomersPagedList(GetPagedPatientInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _patientRepository.GetPagedPatientAsync(input);

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

        [AuthorizePermission("Patient_Delete")]
        public async Task<JsonResult> DeleteCustomer(DeletePatientInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";

                if (await _patientRepository.DeletePatientAsync(input))
                {
                    await _activityLogRepo.LogAsync(input.DeletedBy, "CUSTOMER_DELETE", "Customer", input.ID.ToString(), $"Customer {input.ID} deleted", Request.UserHostAddress);
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
        public async Task<JsonResult> SearchCustomer(SearchPatientInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var resp = await _patientRepository.SearchCustomerAsync(input);

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