using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models.InventoryItems;
using CoreMedixBeta.Models.InventorySettings;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using CoreMedixBeta.Repositories;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class InventorySettingsController : Controller
    {
        private readonly ItemAttributesRepository _itemAttributesRepository;

        public InventorySettingsController() : this(new ItemAttributesRepository())
        {
        }

        public InventorySettingsController(ItemAttributesRepository itemAttributesRepository)
        {
            _itemAttributesRepository = itemAttributesRepository;
        }

        public ActionResult Index() => View();

        public ActionResult ItemAttributes() => View();

        #region ItemAttributes

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage")]
        public async Task<JsonResult> SaveItemAttribute(SaveItemAttributeInputDto formInput)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                string operationType = formInput.ID > 0 ? "UPDATE" : "INSERT";
                ItemAttribute attribute = new ItemAttribute
                {
                    ID = formInput.ID ?? 0,
                    Title = formInput.Title,
                    AttributeType = formInput.Type,
                };
                int Id = await _itemAttributesRepository.SaveAttributeAsync(attribute);
                if (Id <= 0) return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save InventoryItem due to technical error" });

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO { Status = true, Message = "InventoryItem Saved Successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Supplier due to technical error" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetItemAttributes(GetItemAttributesInputDto input)
        {
            try
            {
                GetAllAttributesInputDto getAllAttributesInputDto = new GetAllAttributesInputDto
                {
                };
                var data = await _itemAttributesRepository.GetAllAttributesAsync(getAllAttributesInputDto);

                if (data != null && data.Count > 0)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Data Fetched Successfully", Data = data });
                }

                return Json(new AjaxRequestDTO { Status = true, Message = "Data not found", Data = null });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO { Status = false, Message = "Technical error" });
            }
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage")]
        public async Task<JsonResult> DeleteItemAttribute(DeleteItemAttributeInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                DeleteAttributeInputDto InputDto = new DeleteAttributeInputDto
                {
                    ID = input.ID,
                };

                var isDeleted = await _itemAttributesRepository.DeleteAttributeAsync(InputDto);
                if (isDeleted)
                {
                    var now = ServerConfig.DateTimeNow;

                    return Json(new AjaxRequestDTO { Status = true, Message = "InventoryItem Deleted successfully" });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete InventoryItem due to technical difficulties" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        #endregion ItemAttributes

        #region Invoices Prefix Settings

        [AuthorizePermission("InventorySettings_View")]
        public async Task<ActionResult> InvoicePrefix()
        {
            InvoiceSetting settings = await _itemAttributesRepository.GetInvoiceSettingAsync(new GetInvoiceSettingInputDto
            {
            });
            return View(settings);
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage")]
        public async Task<JsonResult> SaveInvoiceSetting(SaveInvoiceSettingInputDto formInput)
        {
            try
            { 

                string operationType = formInput.ID > 0 ? "UPDATE" : "INSERT";
                InvoiceSetting invoice = new InvoiceSetting
                {
                    ID = formInput.ID,
                    Currency = formInput.Currency,
                    InvoicePrefix = formInput.InvoicePrefix,
                    UserName = formInput.UserName,
                };
                int Id = await _itemAttributesRepository.SaveInvoiceSettingAsync(invoice);
                if (Id <= 0) return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Invoice Setting due to technical error" });

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO { Status = true, Message = "Invoice Setting Saved Successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Invoice Setting due to technical error" });
            }
        }

        #endregion Invoices Prefix Settings

        #region Printer Prefrence Settings

        [AuthorizePermission("InventorySettings_View")]
        public async Task<ActionResult> PrinterPrefrence()
        {
            PrinterPrefrenceVM viewModel = new PrinterPrefrenceVM()
            {
                Prefrence = await _itemAttributesRepository.GetPreferenceAsync(new GetPreferenceInputDto
                {
                })
            };
            return View(viewModel);
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage")]
        public async Task<JsonResult> SavePreference(SavePreferenceInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                string operationType = input.ID > 0 ? "UPDATE" : "INSERT";

                PrinterPrefernce printerPrefernce = new PrinterPrefernce
                {
                    ID = input.ID,
                    Type = input.Type,
                    LastModifiedBy = input.LastModifiedBy,
                };
                var data = await _itemAttributesRepository.SaveAndGetPreferenceAsync(printerPrefernce);
                if (data is null)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save and Get Preference due to technical error" });
                }

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO { Status = true, Data = data, Message = "Data Saved and fetch Successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save preference due to technical error" });
            }
        }

        #endregion Printer Prefrence Settings

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> GetCurrency()
        {
            try
            {
                var data = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                });
                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }
    }
}