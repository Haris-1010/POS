using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Patient.DTOs;
using CoreMedixBeta.Repositories;
using InstaCareEMR.Core.DBHelpers;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
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
            try
            {
                if (string.IsNullOrWhiteSpace(formInput?.Name))
                {
                    return Json(new
                    {
                        Status = false,
                        Message = "Patient name is required"
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
                        Message = "Patients saved successfully",
                        Data = customerId
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    Status = false,
                    Message = "Failed to save Patients due to technical error"
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