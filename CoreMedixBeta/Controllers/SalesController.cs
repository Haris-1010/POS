using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.PurchaseOrders.DTOs;
using CoreMedixBeta.Models.Sales;
using CoreMedixBeta.Models.Sales.DTOs;
using CoreMedixBeta.Models.Supplier.DTOs;
using CoreMedixBeta.Models.Taxes.DTOs;
using CoreMedixBeta.Repositories;
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
    public class SalesController : Controller
    {
        private readonly SupplierRepository _supplierRepository;
        private readonly ManufacturerRepository _manufacturersRepository;
        private readonly TaxesRepository _taxesRepository;
        private readonly ItemAttributesRepository _itemAttributesRepository;
        private readonly SalesRepository _salesRepository;
        private readonly DiscountsRepository _discountsRepository;
        private readonly PatientRepository _patientRepository;

        public SalesController() : this(new SupplierRepository(), new ManufacturerRepository(),
            new TaxesRepository(), new ItemAttributesRepository(),
            new SalesRepository(), new DiscountsRepository(), new PatientRepository())
        {
        }

        public SalesController(
            SupplierRepository supplierRepository,
            ManufacturerRepository manufacturerRepository,
            TaxesRepository taxesRepository,
            ItemAttributesRepository itemAttributesRepository,
            SalesRepository salesRepository,
            DiscountsRepository discountsRepository,
            PatientRepository patientRepository
            )
        {
            _manufacturersRepository = manufacturerRepository;
            _supplierRepository = supplierRepository;
            _taxesRepository = taxesRepository;
            _itemAttributesRepository = itemAttributesRepository;
            _salesRepository = salesRepository;
            _discountsRepository = discountsRepository;
            _patientRepository = patientRepository;
        }

        [AuthorizePermission("Sales_View")]
        public async Task<ActionResult> SaleInvoices(string StartDate, string EndDate, string CustomerID)
        {
            return View(new SaleInvoicesVM
            {
                StartDate = StartDate,
                EndDate = EndDate,
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                }),
                CustomerId = CustomerID
            });
        }

        [AuthorizePermission("Sales_View", "Sales_Add")]
        public async Task<ActionResult> ManageInvoice(GetSaleInvoiceInputDto input)
        {
            try
            {
                ManageInvoiceVM viewModel = new ManageInvoiceVM
                {
                    InvoiceID = input.InvoiceID,
                };
                viewModel.Taxes = await _taxesRepository.GetTaxesShortListAsync(new TaxesShortListInputDto
                {
                });
                viewModel.Discounts = await _discountsRepository.GetDiscountsShortListAsync(new RequestByMasterUsernameDTO
                {
                });
                viewModel.Manufacturers = await _manufacturersRepository.GetManufacturerShortList();
                viewModel.Suppliers = await _supplierRepository.GetSupplierShortList();
                viewModel.Patient = new Patients();
                viewModel.Cities = await _patientRepository.GetAllCitiesNamesAsync();
                if (input.InvoiceID > 0)
                {
                    GetSaleInvoiceInputDto getSaleInvoice = new GetSaleInvoiceInputDto
                    {
                        InvoiceID = input.InvoiceID,
                        IsPrint = input.IsPrint
                    };
                    viewModel.Invoice = await _salesRepository.GetSaleInvoice(getSaleInvoice);
                    viewModel.InvoiceID = input.InvoiceID;
                }
                else
                {
                    viewModel.InvoicePrefix = await _itemAttributesRepository.GetInvoicePrefixAsync(new GetInvoiceSettingInputDto
                    {
                    });
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return View(new ManageInvoiceVM());
            }
        }

        [AuthorizePermission("Sales_View", "Sales_Edit")]
        public async Task<ActionResult> ManageInvoiceReturn(int ID)
        {
            try
            {
                ManageInvoiceVM viewModel = new ManageInvoiceVM();
                viewModel.Taxes = await _taxesRepository.GetTaxesShortListAsync(new TaxesShortListInputDto
                {
                });
                viewModel.Discounts = await _discountsRepository.GetDiscountsShortListAsync(new RequestByMasterUsernameDTO
                {
                });
                viewModel.Manufacturers = await _manufacturersRepository.GetManufacturerShortList();

                viewModel.Invoice = await _salesRepository.GetSaleInvoice(new GetSaleInvoiceInputDto
                {
                    InvoiceID = ID,
                });
                viewModel.InvoiceID = ID;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return View(new ManageInvoiceVM());
            }
        }

        [AuthorizePermission("Sales_View")]
        public async Task<ActionResult> PrintInvoice(GetSaleInvoiceInputDto input)
        {
            if (input.InvoiceID <= 0)
            {
                return View("SaleInvoices");
            }
            GetSaleInvoiceInputDto getSaleInvoice = new GetSaleInvoiceInputDto
            {
                InvoiceID = input.InvoiceID,
                IsPrint = input.IsPrint
            };
            var viewModel = new ManageInvoiceVM
            {
                Invoice = await _salesRepository.GetSaleInvoice(getSaleInvoice),
                InvoiceID = input.InvoiceID
            };

            if (viewModel.InvoiceID > 0)
            {
                return View(viewModel);
            }

            return View("SaleInvoices");
        }

        [HttpPost]
        [AuthorizePermission("Sales_Add", "Sales_Edit")]
        public async Task<JsonResult> SaveSale(SaveSaleInputDto input)
        {
            try
            {
                string operationType = input.SaleInvoiceItems.Any(i => i.TransactionStatus?.ToUpper() == "RETURNED") ?
                   "RETURN" : (input.SaleInvoice.ID > 0 ? "UPDATE" : "INSERT");

                if (input.SaleInvoicePayments != null && input.SaleInvoicePayments.Count > 0)
                {
                    foreach (var payment in input.SaleInvoicePayments)
                    {
                        if (string.IsNullOrWhiteSpace(payment.AttachmentURL))
                            continue;

                        if (IsBase64String(payment.AttachmentURL))
                        {
                            byte[] bytes = Convert.FromBase64String(payment.AttachmentURL);
                            var inputStream = new MemoryStream(bytes);

                            string fileAddress = $"Payment_Sale_{Guid.NewGuid()}.png";
                            string folderName = BlobUploader.GetFolderName("Images");
                            string saveLocation = $"{ServerConfig.BlobPath}{folderName}/{fileAddress}";
                            await Task.Run(() => BlobUploader.UploadTo(folderName, fileAddress, inputStream));
                            payment.AttachmentURL = saveLocation;
                        }
                    }
                }
                SaveSaleInputDto saveSaleInputDto = new SaveSaleInputDto
                {
                    SaleInvoice = input.SaleInvoice,
                    SaleInvoiceFinancialDetails = input.SaleInvoiceFinancialDetails,
                    SaleInvoiceItems = input.SaleInvoiceItems,
                    SaleInvoicePayments = input.SaleInvoicePayments,
                };

                string currentUsername = Session["Username"]?.ToString();
                saveSaleInputDto.CurrentUsername = currentUsername;
                if (!string.IsNullOrWhiteSpace(currentUsername) && saveSaleInputDto.SaleInvoice != null)
                {
                    saveSaleInputDto.SaleInvoice.LastModifiedBy = currentUsername;

                    if (saveSaleInputDto.SaleInvoice.ID == 0)
                    {
                        saveSaleInputDto.SaleInvoice.CreatedBy = currentUsername;

                        if (string.IsNullOrWhiteSpace(saveSaleInputDto.SaleInvoice.SalePersonUsername))
                        {
                            saveSaleInputDto.SaleInvoice.SalePersonUsername = currentUsername;
                        }
                    }
                }

                // Patient select nahi hua aur naam type kiya hai to invoice ke saath hi patient auto-add ho jaye.
                if (saveSaleInputDto.SaleInvoice != null && saveSaleInputDto.SaleInvoice.ID == 0)
                {
                    string customerId = (saveSaleInputDto.SaleInvoice.CustomerID ?? string.Empty).Trim();
                    string typedName = (saveSaleInputDto.SaleInvoice.SearchCustomer ?? string.Empty).Trim();

                    if ((customerId.Length == 0 || customerId == "0") && typedName.Length > 0
                        && !string.Equals(typedName, "Add New Patient", StringComparison.OrdinalIgnoreCase))
                    {
                        int patientId = await _patientRepository.FindPatientIdByNameAsync(typedName);

                        if (patientId <= 0)
                        {
                            patientId = await _patientRepository.SavePatientsAsync(new Patients
                            {
                                Name = typedName,
                                CreatedBy = string.IsNullOrWhiteSpace(currentUsername) ? "System" : currentUsername
                            });
                        }

                        if (patientId > 0)
                        {
                            saveSaleInputDto.SaleInvoice.CustomerID = patientId.ToString();
                            if (string.IsNullOrWhiteSpace(saveSaleInputDto.SaleInvoice.CustomerType))
                            {
                                saveSaleInputDto.SaleInvoice.CustomerType = "Patient";
                            }
                        }
                    }
                }

                int id = await _salesRepository.SaveSaleAsync(saveSaleInputDto);

                if (id <= 0) return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Sale Invoice due to technical error" });

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO { Status = true, Message = "Sale Invoice Saved Successfully", Data = id });
            }
            catch (Exception ex)
            {
                return Json(new AjaxRequestDTO() { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizePermission("Sales_Delete")]
        public async Task<JsonResult> DeleteSale(DeleteSaleInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                ManageInvoiceVM viewModel = new ManageInvoiceVM();
                viewModel.Invoice = await _salesRepository.GetSaleInvoice(new GetSaleInvoiceInputDto
                {
                    InvoiceID = input.ID,
                });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";
                if (await _salesRepository.DeleteSaleAsync(input))
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Sale Invoice Deleted successfully", });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Sale Invoice due to technical Error" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        [AuthorizePermission("Sales_View")]
        public async Task<JsonResult> GetPagedSale(GetPagedSalesInputDto input)
        {
            try
            {
                if (input.DataTableValues == null)
                {
                    input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                }
                var data = await _salesRepository.GetPagedSalesAsync(input);

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
        public async Task<JsonResult> GetSalesInfo()
        {
            try
            {
                var data = await _salesRepository.GetSalesInfoAsync(new GetSaleInvoiceInputDto
                {
                });

                if (data is null) return Json(new AjaxRequestDTO());

                return Json(new AjaxRequestDTO { Status = true, Message = "Success", Data = data });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetInvoicePrintShortURL(int ID)
        {
            try
            {
                string url = Url.Action("PrintSaleInvoice", "PrintPublic", new { ID = ID }, Request.Url.Scheme);
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
        public async Task<JsonResult> SendInvoiceToCustomer(SendInvoiceToCustomerDto input)
        {
            try
            {
                if (!ModelState.IsValid) return Json(new AjaxRequestDTO { Message = "Invalid Details", Status = false });
                string url = Url.Action("PrintSaleInvoice", "PrintPublic", new { id = input.ID }, Request.Url.Scheme);

                string shortUrl = url;

                return Json(new AjaxRequestDTO { Message = "Invoice Sent Successfully", Data = shortUrl, Status = true });
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

        [HttpPost]
        public async Task<JsonResult> GetLastInvoiceByCustomer(int customerId)
        {
            try
            {
                var lastInvoice = await _salesRepository.GetLastInvoiceByCustomerAsync(customerId);

                if (lastInvoice == null)
                    return Json(new { Status = false, Message = "No invoices found" });

                return Json(new
                {
                    Status = true,
                    Data = new
                    {
                        lastInvoice.ID,
                        lastInvoice.InvoiceNo,
                        InvoiceDate = DateTime.Parse(lastInvoice.InvoiceDate).ToString("MM/dd/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { Status = false, Message = ex.Message });
            }
        }
    }
}