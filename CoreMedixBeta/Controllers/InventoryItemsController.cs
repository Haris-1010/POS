using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.InventoryItems;
using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Models.ManufacturerBrand;
using CoreMedixBeta.Models.Menufacturer.DTOs;
using CoreMedixBeta.Models.Supplier;
using CoreMedixBeta.Models.Supplier.DTOs;
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
    public class InventoryItemsController : Controller
    {
        private readonly SupplierRepository _supplierRepository;
        private readonly PatientRepository _patientRepository;
        private readonly ManufacturerRepository _manufacturerRepository;
        private readonly ManufacturerBrandsRepository _brandsRepository;
        private readonly InventoryItemsRepository _inventoryItemsRepository;

        public InventoryItemsController() : this(new SupplierRepository(), new PatientRepository(), new ManufacturerRepository(), new ManufacturerBrandsRepository(), new InventoryItemsRepository())
        {
        }

        public InventoryItemsController(
            SupplierRepository supplierRepository,
            PatientRepository patientRepository,
            ManufacturerRepository manufacturerRepository,
            ManufacturerBrandsRepository manufacturerBrandsRepository,
            InventoryItemsRepository inventoryItemsRepository
            )
        {
            _manufacturerRepository = manufacturerRepository;
            _supplierRepository = supplierRepository;
            _patientRepository = patientRepository;
            _brandsRepository = manufacturerBrandsRepository;
            _inventoryItemsRepository = inventoryItemsRepository;
        }

        [AuthorizePermission("Inventory_View")]
        public async Task<ActionResult> Items(string FilterType)
        {
            ItemsViewModel viewModel = new ItemsViewModel
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturerRepository.GetManufacturerShortList(),
                FilterType = FilterType
            };
            return View(viewModel);
        }

        [AuthorizePermission("Inventory_View", "Inventory_Add", "Inventory_Edit")]
        public async Task<ActionResult> ManageItem(ManageItemInputDto input)
        {
            ManageItemViewModel viewModel = new ManageItemViewModel
            {
                AllAttributes = await _inventoryItemsRepository.GetAllAttributesAsync(input),
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturerRepository.GetManufacturerShortList(),
                Cities = await _patientRepository.GetAllCitiesNamesAsync(),
                ItemID = input.ItemID
            };

            if (input.ItemID > 0)
            {
                viewModel.ItemID = input.ItemID;
                viewModel.ManufacturerBrands = await _brandsRepository.GetBrandShortList(new GetPagedBrandsInputDto
                {
                    ManufacturerID = input.ManufacturerID
                });
                viewModel.Item = await _inventoryItemsRepository.GetItemByIdAsync(new GetItemByIdInputDto
                {
                    ID = input.ItemID
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<JsonResult> GetInventoryItemsInfo()
        {
            try
            {
                var data = await _inventoryItemsRepository.GetInventoryItmesInfoAsync();

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
        [AuthorizePermission("Inventory_Add", "Inventory_Edit")]
        public async Task<JsonResult> SaveInventoryItem(SaveInventoryItemInputDto formInput)
        {
            try
            {
                if (formInput.SupplierID == "0" || formInput.ManufacturerID == 0)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Item due to technical error" });
                }
                string operationType = formInput.ID > 0 ? "UPDATE" : "INSERT";
                string currentUsername = Session["Username"]?.ToString() ?? "System";

                var entity = new InventoryItem
                {
                    ID = formInput.ID,
                    Name = formInput.Name,
                    ProductType = formInput.ProductType,
                    MedicineSalt = formInput.MedicineSalt,
                    SupplierID = formInput.SupplierID,
                    ManufacturerID = formInput.ManufacturerID,
                    BrandID = formInput.BrandID,
                    LastModifiedBy = currentUsername,
                    CreatedBy = currentUsername,
                };

                int ID = await _inventoryItemsRepository.SaveItemAsync(entity);
                var data = new { ID = ID, ManufacturerID = formInput.ManufacturerID };
                return Json(new AjaxRequestDTO { Status = true, Message = "InventoryItem Saved Successfully", Data = data });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Item due to technical error" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedInventoryItems(GetPagedInventoryItemsInputDto input)
        {
            try
            {
                var inputObj = new GetPagedItemsInputDto
                {
                    SKU = input.SKU,
                    UPC = input.UPC,
                    UniqueID = input.UniqueID,
                    ProductType = input.ProductType,
                    CategoryID = input.CategoryID,
                    ManufacturerID = input.ManufacturerID,
                    SupplierID = input.SupplierID,
                    BrandID = input.BrandID,
                    FilterType = input.FilterType,
                    SortBy = input.SortBy,
                    SortByOrder = input.SortByOrder
                };
                inputObj.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _inventoryItemsRepository.GetPagedItemsAsync(inputObj);

                return Json(new PagedAjaxRequestOutputDTO
                {
                    draw = Request.Form.GetValues("draw").FirstOrDefault(),
                    recordsFiltered = data?.Item2 ?? string.Empty,
                    recordsTotal = data?.Item2 ?? string.Empty,
                    data = data?.Item1 ?? "[]"
                });
            }
            catch (Exception)
            {
                return Json(new PagedAjaxRequestOutputDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetInventoryItemByID(GetItemByIdInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });
                var data = await _inventoryItemsRepository.GetItemByIdAsync(input);

                if (data is null)
                {
                    return Json(new AjaxRequestDTO { Status = false });
                }
                return Json(new AjaxRequestDTO { Status = true, Data = data });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        [AuthorizePermission("Inventory_Delete")]
        public async Task<JsonResult> DeleteInventoryItem(DeleteItemInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";

                if (await _inventoryItemsRepository.DeleteItemAsync(input))
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Item Deleted successfully" });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Item due to technical difficulties" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        [AuthorizePermission("Inventory_Add")]
        public async Task<JsonResult> ImportItems(ImportItemsInputDto input)
        {
            try
            {
                if (input == null || input.Rows == null || input.Rows.Count == 0)
                    return Json(new AjaxRequestDTO { Status = false, Message = "No rows found in the selected file" });

                if (input.Rows.Count > 1000)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Maximum 1000 rows can be imported at a time" });

                string username = Session["Username"]?.ToString() ?? "System";
                var result = await _inventoryItemsRepository.ImportItemsAsync(input.Rows, username);

                string message = "Imported " + result.VariationsCreated + " variation(s) under " + (result.ItemsCreated + result.ItemsReused) + " item(s)";
                if (result.Skipped > 0) message += ", " + result.Skipped + " skipped (already exists)";
                if (result.Failed > 0) message += ", " + result.Failed + " failed";
                message += ".";

                bool hasWork = result.VariationsCreated > 0 || result.Skipped > 0;
                return Json(new AjaxRequestDTO { Status = hasWork, Message = message, Data = result });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        [AuthorizePermission("Inventory_Delete")]
        public async Task<JsonResult> BulkDeleteItems(BulkDeleteItemsInputDto input)
        {
            try
            {
                if (input == null)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                bool hasIds = input.IDs != null && input.IDs.Count > 0;
                bool hasIdentifiers = input.Identifiers != null && input.Identifiers.Any(x => !string.IsNullOrWhiteSpace(x));

                if (!hasIds && !hasIdentifiers)
                    return Json(new AjaxRequestDTO { Status = false, Message = "No items selected for deletion" });

                string username = Session["Username"]?.ToString() ?? "System";
                var result = await _inventoryItemsRepository.BulkDeleteItemsAsync(input, username);

                if (result.DeletedItems > 0)
                {
                    string message = result.DeletedItems + " item(s) deleted successfully";
                    if (result.NotMatched > 0) message += ". " + result.NotMatched + " row(s) did not match any item";
                    return Json(new AjaxRequestDTO { Status = true, Message = message, Data = result });
                }

                string failureMessage = "No items were deleted";
                if (result.NotMatched > 0) failureMessage = "No matching items found for " + result.NotMatched + " row(s)";

                return Json(new AjaxRequestDTO { Status = false, Message = failureMessage, Data = result });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetBulkItems(BulkItemsInputDto input)
        {
            try
            {
                var data = await _inventoryItemsRepository.GetBulkItemsAsync(input ?? new BulkItemsInputDto());
                if (data != null && data.Count > 0)
                    return Json(new AjaxRequestDTO { Status = true, Message = "Success", Data = data });

                return Json(new AjaxRequestDTO { Status = false, Message = "Data Not Found" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> ResolveBulkItems(ResolveBulkItemsInputDto input)
        {
            try
            {
                if (input == null || input.Keys == null || input.Keys.Count == 0)
                    return Json(new AjaxRequestDTO { Status = false, Message = "No rows found in the selected file" });

                var result = await _inventoryItemsRepository.ResolveBulkItemsAsync(input.Keys);

                string message = result.Items.Count + " item(s) matched";
                if (result.Unresolved.Count > 0) message += ", " + result.Unresolved.Count + " could not be matched";

                return Json(new AjaxRequestDTO { Status = result.Items.Count > 0, Message = message, Data = result });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> SearchItems(SearchItemsInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var data = await _inventoryItemsRepository.SearchItemsAsync(input);
                if (data != null && data.Count > 0)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Success", Data = data });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Data Not Found" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetItemsByReferralOrder(SearchItemsInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var data = await _inventoryItemsRepository.GetItemsByReferralOrderAsync(input);
                if (data != null && data.Count > 0)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Success", Data = data });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Data Not Found" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveItemVariation(InventoryItemVariation formInput)
        {
            try
            {
                string operationType = formInput.ID > 0 ? "UPDATE" : "INSERT";

                formInput.CreatedBy = Session["Username"]?.ToString() ?? "System";
                formInput.LastModifiedBy = formInput.CreatedBy;

                var response = await _inventoryItemsRepository.SaveItemVariationAsync(formInput);
                if (response.Value > 0)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Variation Saved Successfully" });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = response.FirstError.Description });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Item due to technical error" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteVariation(DeleteInventoryItemInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                input.DeletedBy = Session["Username"]?.ToString() ?? "System";

                if (await _inventoryItemsRepository.DeleteVariationAsync(input))
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Variation Deleted successfully" });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Variation due to technical difficulties" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedVariations(GetPagedItemVariationsInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _inventoryItemsRepository.GetPagedItemVariationsAsync(input);

                return Json(new PagedAjaxRequestOutputDTO
                {
                    draw = Request.Form.GetValues("draw").FirstOrDefault(),
                    recordsFiltered = data?.Item2 ?? string.Empty,
                    recordsTotal = data?.Item2 ?? string.Empty,
                    data = data?.Item1 ?? "[]"
                });
            }
            catch (Exception)
            {
                return Json(new PagedAjaxRequestOutputDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetVariationByID(GetItemByIdInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var data = await _inventoryItemsRepository.GetVariationByIdAsync(input);

                if (data is null || data.ID is 0)
                {
                    return Json(new AjaxRequestDTO { Status = false });
                }

                return Json(new AjaxRequestDTO { Status = true, Data = data });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveVariationImages(List<InventoryItemVariationImage> Images)
        {
            try
            {
                if (!ModelState.IsValid || Images == null || Images.Count == 0)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });
                }

                foreach (var image in Images)
                {
                    if (string.IsNullOrWhiteSpace(image.ImageURL))
                        continue;

                    if (IsBase64String(image.ImageURL))
                    {
                        byte[] bytes = Convert.FromBase64String(image.ImageURL);
                        var inputStream = new MemoryStream(bytes);

                        string cleanImageName = Path.GetFileNameWithoutExtension(image.ImageName).Replace(" ", "");
                        string fileAddress = $"{cleanImageName}_Variation{image.VariationID}_{Guid.NewGuid()}.png";
                        string folderName = BlobUploader.GetFolderName("Images");
                        string saveLocation = $"{ServerConfig.BlobPath}{folderName}/{fileAddress}";
                        await Task.Run(() => BlobUploader.UploadTo(folderName, fileAddress, inputStream));
                        image.ImageURL = saveLocation;
                    }
                }

                bool isSaved = await _inventoryItemsRepository.SaveVariationImagesAsync(Images);

                if (isSaved)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Variation Images Saved Successfully" });
                }

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Images due to technical error" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Images due to technical error" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetVariationImages(GetItemByIdInputDto input)
        {
            try
            {
                var data = await _inventoryItemsRepository.GetVariationImagesAsync(input);

                if (data is null || data.Count is 0)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Images Not Found" });
                }

                return Json(new AjaxRequestDTO { Status = true, Data = data, Message = "Success" });
            }
            catch (Exception)
            {
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

        [HttpGet]
        public async Task<JsonResult> GenerateBarcode()
        {
            try
            {
                var barcode = await _inventoryItemsRepository.GenerateUniqueBarcodeAsync();
                if (!string.IsNullOrEmpty(barcode))
                {
                    return Json(new AjaxRequestDTO { Status = true, Data = barcode, Message = "Barcode generated successfully" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to generate barcode" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> SearchManufacturer(SearchManufacturerInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var resp = await _manufacturerRepository.SearchManufacturerAsync(input);

                return Json(new AjaxRequestDTO { Data = resp, Status = true, Message = "Success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> SearchSupplier(SearchSupplierInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var resp = await _supplierRepository.SearchSupplierAsync(input);

                return Json(new AjaxRequestDTO { Data = resp, Status = true, Message = "Success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetItemActivityLogs(GetItemActivityLogInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                var data = await _inventoryItemsRepository.GetListItemActivityLogsAsync(input);
                if (data != null && data.Count > 0)
                {
                    return Json(new AjaxRequestDTO { Status = true, Message = "Success", Data = data });
                }
                return Json(new AjaxRequestDTO { Status = false, Message = "Data Not Found" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }
    }
}