using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Menufacturer;
using CoreMedixBeta.Models.Menufacturer.DTOs;
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
    public class ManufacturersController : Controller
    {
        private readonly PatientRepository _patientRepository;
        private readonly ManufacturerRepository _manufacturerRepository;

        public ManufacturersController() : this(new PatientRepository(), new ManufacturerRepository())
        {
        }

        public ManufacturersController(PatientRepository patientRepository, ManufacturerRepository manufacturerRepository)
        {
            _patientRepository = patientRepository;
            _manufacturerRepository = manufacturerRepository;
        }

        public async Task<ActionResult> Manufacturers()
        {
            return View(new ManufacturersVM
            {
                Cities = await _patientRepository.GetAllCitiesNamesAsync()
            });
        }

        [HttpPost]
        [AuthorizePermission("Manufacturers_Add", "Manufacturers_Edit")]
        public async Task<JsonResult> SaveManufacturer(Manufacturer formInput)
        {
            try
            {
                string currentUsername = Session["Username"]?.ToString() ?? "System";
                formInput.CreatedBy = currentUsername;
                formInput.LastModifiedBy = currentUsername;

                var customerId = await _manufacturerRepository.SaveManufacturerAsync(formInput);

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
        [AuthorizePermission("Manufacturers_View")]
        public async Task<JsonResult> GetPagedManufacturers(GetPagedManufacturersInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _manufacturerRepository.GetPagedManufacturersAsync(input);

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
        [AuthorizePermission("Manufacturers_Delete")]
        public async Task<JsonResult> DeleteManufacturer(DeleteManufacturerInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";

                if (await _manufacturerRepository.DeleteManufacturerAsync(input))
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
    }
}