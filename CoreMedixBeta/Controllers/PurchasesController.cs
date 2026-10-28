using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using CoreMedixBeta.Models.PurchaseOrders;
using CoreMedixBeta.Models.PurchaseOrders.DTOs;
using CoreMedixBeta.Models.Purchases.DTOs;
using CoreMedixBeta.Models.Supplier.DTOs;
using CoreMedixBeta.Models.Taxes.DTOs;
using CoreMedixBeta.Repositories;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class PurchasesController : Controller
    {
        private readonly SupplierRepository _supplierRepository;
        private readonly ManufacturerRepository _manufacturersRepository;
        private readonly TaxesRepository _taxesRepository;
        private readonly ItemAttributesRepository _itemAttributesRepository;
        private readonly PurchasesRepository _purchasesRepository;
        private readonly PurchaseOrdersRepository _purchaseOrdersRepository;
        private readonly ActivityLogRepository _activityLogRepo = new ActivityLogRepository();

        public PurchasesController() : this(new SupplierRepository(), new ManufacturerRepository(), new TaxesRepository(), new ItemAttributesRepository(), new PurchasesRepository(), new PurchaseOrdersRepository())
        {
        }

        public PurchasesController(
            SupplierRepository supplierRepository,
            ManufacturerRepository manufacturerRepository,
            TaxesRepository taxesRepository,
            ItemAttributesRepository itemAttributesRepository,
            PurchasesRepository purchasesRepository,
            PurchaseOrdersRepository purchaseOrdersRepository
            )
        {
            _manufacturersRepository = manufacturerRepository;
            _supplierRepository = supplierRepository;
            _taxesRepository = taxesRepository;
            _itemAttributesRepository = itemAttributesRepository;
            _purchasesRepository = purchasesRepository;
            _purchaseOrdersRepository = purchaseOrdersRepository;
        }

        [AuthorizePermission("Purchases_View")]
        public async Task<ActionResult> PurchaseOrders()
        {
            return View(new PurchaseOrdersVM
            {
                SuppliersShortList = await _supplierRepository.GetSupplierShortList(),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [AuthorizePermission("Purchases_View", "Purchases_Add")]
        public async Task<ActionResult> ManagePurchaseOrder(ManagePurchaseOrderDto input)
        {
            var viewModel = new ManagePurchaseOrderVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                ReferenceNo = await _purchaseOrdersRepository.GenerateReferenceNoAsync(new RequestByMasterUsernameDTO { })
            };
            if (input.ID > 0)
            {
                viewModel.DetailedOrder = await _purchaseOrdersRepository.GetDetailedPurchaseOrder(
                    new GetDetailedPurchaseOrderInputDto
                    {
                        ID = input.ID
                    });
                viewModel.PurchaseOrderID = input.ID;
            }
            return View(viewModel);
        }

        [HttpPost]
        [AuthorizePermission("Purchases_Add", "Purchases_Edit")]
        public async Task<JsonResult> SavePurchaseOrder(SavePurchaseOrderDto formInput)
        {
            try
            {
                string operationType = formInput.Order.ID > 0 ? "UPDATE" : "INSERT";
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO() { Status = false, Message = "Invalid Data" });
                SavePurchaseOrderInputDto orderInputDto = new SavePurchaseOrderInputDto
                {
                    Order = formInput.Order,
                    OrderItems = formInput.OrderItems,
                    CurrentUsername = Session["Username"]?.ToString(),
                };
                var id = await _purchaseOrdersRepository.SavePurchaseOrder(orderInputDto);

                if (id > 0)
                {
                    await _activityLogRepo.LogAsync(Session["Username"]?.ToString() ?? "System", formInput.Order.ID > 0 ? "PURCHASE_ORDER_UPDATE" : "PURCHASE_ORDER_SAVE", "PurchaseOrder", id.ToString(), $"Purchase Order {formInput.Order.ReferenceNo} saved", Request.UserHostAddress);
                    return Json(new AjaxRequestDTO { Status = true, Message = "Purchase Order Saved Successfully", Data = id });
                }
                else
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Purchase Order due to technical error" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO() { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedPurchaseOrders(GetPagedPurchasesInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);

                GetPagedPurchaseOrdersInputDto ordersInputDto = new GetPagedPurchaseOrdersInputDto
                {
                    ReferenceNo = input.ReferenceNo,
                    SupplierID = input.SupplierID,
                    FilterType = input.FilterType,
                    Status=input.Status,
                    SortByOrder=input.SortByOrder,
                    SortBy=input.SortBy,
                    DataTableValues = input.DataTableValues
                };

                var data = await _purchaseOrdersRepository.GetPagedPurchaseOrders(ordersInputDto);

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
        public async Task<JsonResult> GetPurchaseOrdersInfo()
        {
            try
            {
                var data = await _purchaseOrdersRepository.GetGetPurchaseOrdersInfoAsync();

                if (data is null) return Json(new AjaxRequestDTO());

                return Json(new AjaxRequestDTO { Status = true, Message = "Success", Data = data });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        [AuthorizePermission("Purchases_Delete")]
        public async Task<JsonResult> DeletePurchaseOrder(DeleteRequestDTO input)
        {
            try
            {
                if (input.ID <= 0)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";

                if (await _purchaseOrdersRepository.DeletePurchaseOrder(input))
                {
                    await _activityLogRepo.LogAsync(input.DeletedBy, "PURCHASE_DELETE", "Purchase", input.ID.ToString(), $"Purchase {input.ID} deleted", Request.UserHostAddress);
                    return Json(new AjaxRequestDTO { Status = true, Message = "Purchase Deleted successfully", });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Purchase due to technical Error" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [AuthorizePermission("Purchases_View")]
        public async Task<ActionResult> ReceivedPurchases()
        {
            return View(new PurchaseOrdersVM
            {
                SuppliersShortList = await _supplierRepository.GetSupplierShortList(),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [AuthorizePermission("Purchases_View", "Purchases_Edit")]
        public async Task<ActionResult> ManagePurchase(ManagePurchaseInputDto input)
        {
            var viewModel = new ManagePurchaseVM
            {
                Taxes = await _taxesRepository.GetTaxesShortListAsync(new TaxesShortListInputDto
                {
                })
            };

            viewModel.PurchaseOrderID = input.PurchaseOrderID;
            viewModel.DetailedOrder = await _purchaseOrdersRepository.GetDetailedPurchaseOrder(new GetDetailedPurchaseOrderInputDto
            {
                ID = input.PurchaseOrderID,
            });
            if (input.ID > 0)
            {
                viewModel.Purchase = await _purchasesRepository.GetDetailedPurchases(new GetDetailedPurchasesInputDto
                {
                    ID = input.ID,
                    PurchaseOrderID = input.PurchaseOrderID,
                });
                viewModel.PurchaseID = input.ID;
            }

            return View(viewModel);
        }

        [HttpPost]
        [AuthorizePermission("Purchases_Add", "Purchases_Edit")]
        public async Task<JsonResult> SavePurchase(SavePurchaseInputDto input)
        {
            try
            {
                string operationType = input.Purchase.ID > 0 ? "UPDATE" : "INSERT";
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO() { Status = false, Message = "Invalid Data" });

                input.CurrentUsername = Session["Username"]?.ToString();

                if (input.PurchasePayments != null && input.PurchasePayments.Count > 0)
                {
                    foreach (var payment in input.PurchasePayments)
                    {
                        if (string.IsNullOrWhiteSpace(payment.AttachmentURL))
                            continue;

                        if (IsBase64String(payment.AttachmentURL))
                        {
                            byte[] bytes = Convert.FromBase64String(payment.AttachmentURL);
                            var inputStream = new MemoryStream(bytes);

                            string fileAddress = $"Payment_Purchase_{Guid.NewGuid()}.png";
                            string folderName = BlobUploader.GetFolderName("Images");
                            string saveLocation = $"{ServerConfig.BlobPath}{folderName}/{fileAddress}";
                            await Task.Run(() => BlobUploader.UploadTo(folderName, fileAddress, inputStream));
                            payment.AttachmentURL = saveLocation;
                        }
                    }
                }

                var id = await _purchasesRepository.SavePurchase(input);
                if (id > 0)
                {
                    await _activityLogRepo.LogAsync(Session["Username"]?.ToString() ?? "System", "PURCHASE_RECEIVE", "Purchase", id.ToString(), $"Purchase {id} saved", Request.UserHostAddress);
                    return Json(new AjaxRequestDTO { Status = true, Message = "Purchase Order Saved Successfully", Data = id });
                }
                else
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Purchase Order due to technical error" });
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO() { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizePermission("Purchases_Edit")]
        public async Task<JsonResult> ClearDueAmount(ClearDueAmountInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO() { Status = false, Message = "Invalid Data" });

                if (string.IsNullOrWhiteSpace(input.LastModifiedBy))
                    input.LastModifiedBy = Session["Username"]?.ToString() ?? "System";

                var id = await _purchaseOrdersRepository.ClearDueAmountAsync(input);

                if (id > 0)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Due Cleared Amount Updated Successfully", Data = id });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Update Due Cleared Amount due to technical error" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO() { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedPurchases(GetPagedPurchasesInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _purchasesRepository.GetPagedPurchases(input);

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

        [HttpGet]
        public async Task<JsonResult> GetPurchasePrintShortURL(int ID)
        {
            try
            {
                string url = Url.Action("PrintPurchaseOrder", "PrintPublic", new { id = ID }, Request.Url.Scheme);

                string shortUrl = url;
                return Json(new AjaxRequestDTO { Message = "Success", Data = shortUrl, Status = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> SendPurchaseOrderToSupplier(SendInvoiceToCustomerDto input)
        {
            try
            {
                if (!ModelState.IsValid) return Json(new AjaxRequestDTO { Message = "Invalid Details", Status = false });
                string url = Url.Action("PrintPurchaseOrder", "PrintPublic", new { id = input.ID }, Request.Url.Scheme);

                string shortUrl = url;
                return Json(new AjaxRequestDTO { Message = "Purchase Order Sent Successfully", Data = shortUrl, Status = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            if (base64.StartsWith(ServerConfig.BlobPath, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }
}