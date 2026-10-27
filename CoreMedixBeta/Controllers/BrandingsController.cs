using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Brandings;
using CoreMedixBeta.Models.Brandings.DTOs;
using CoreMedixBeta.Models.Discounts.DTOs;
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
    public class BrandingsController : Controller
    {
        private readonly BrandingsRepository _brandingRepo;

        public BrandingsController() : this(new BrandingsRepository())
        {
        }

        public BrandingsController(BrandingsRepository brandingRepository)
        {
            _brandingRepo = brandingRepository;
        }

        public ActionResult PrintBranding()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetPrintBranding()
        {
            try
            {
                var data = await _brandingRepo.GetPrintBrandingAsync(new GetPrintBrandingInputDto
                {
                });

                if (data is null || data.ID is 0)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "Data Not Found" });
                }

                return Json(new AjaxRequestDTO { Status = true, Data = data, Message = "Success" });
            }
            catch (Exception)
            {
                return Json(new AjaxRequestDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> SavePrintBranding(SavePrintBrandingInputDto input)
        {
            try
            { 

                string operationType = input.ID > 0 ? "UPDATE" : "INSERT";

                if (input.BrandingImages != null && input.BrandingImages.Count > 0)
                {
                    foreach (var img in input.BrandingImages.Where(img => IsBase64String(img.ImageURL)))
                    {
                        string imageTypeProperty = null;

                        switch (img.Type)
                        {
                            case PrintBrandingImagesType.LOGO:
                                imageTypeProperty = nameof(input.Logo);
                                break;

                            case PrintBrandingImagesType.HEADER:
                                imageTypeProperty = nameof(input.HeaderImg);
                                break;

                            case PrintBrandingImagesType.FOOTER:
                                imageTypeProperty = nameof(input.FooterImg);
                                break;

                            default:
                                imageTypeProperty = null;
                                break;
                        }
                        if (imageTypeProperty == null) continue;

                        byte[] bytes = Convert.FromBase64String(img.ImageURL);
                        var inputStream = new MemoryStream(bytes);

                        string cleanImageName = Path.GetFileNameWithoutExtension(img.ImageName).Replace(" ", "");
                        string fileAddress = $"{cleanImageName}_PrintBranding{img.ImageName}_{Guid.NewGuid()}.png";
                        string folderName = BlobUploader.GetFolderName("Images");
                        string saveLocation = $"{ServerConfig.BlobPath}{folderName}/{fileAddress}"; 
                        BlobUploader.UploadTo(folderName, fileAddress, inputStream);
                        typeof(SavePrintBrandingInputDto).GetProperty(imageTypeProperty)?.SetValue(input, saveLocation);
                    }
                }
                PrintBranding print = new PrintBranding
                {
                    ID = input.ID,
                    BusinessName = input.BusinessName,
                    BusinessPhone = input.BusinessPhone,
                    BusinessEmail = input.BusinessEmail,
                    HeaderImg = input.HeaderImg,
                    HeaderText = input.HeaderText,
                    FooterImg = input.FooterImg,
                    FooterText = input.FooterText,
                    Logo = input.Logo,
                    LastModifiedBy = input.LastModifiedBy
                };
                var response = await _brandingRepo.SavePrintBrandingAsync(print);

                if (response.ID <= 0) return Json(new AjaxRequestDTO
                {
                    Status = false,
                    Message = "Failed to Save Branding",
                    Data = response
                });

                BrandingsRepository.ClearPrintLogoCache();

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Message = "Branding Saved Successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetPrintBrandingCustomField()
        {
            try
            {
                var data = await _brandingRepo.GetPrintBrandingCustomFieldListAsync(new PagedRequestInputDto
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
            catch (Exception)
            {
                return Json(new PagedAjaxRequestOutputDTO());
            }
        }

        [HttpPost]
        public async Task<JsonResult> SavePrintBrandingCustomField(PrintBrandingCustomField input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                string operationType = input.ID > 0 ? "UPDATE" : "INSERT";

                var response = await _brandingRepo.SavePrintBrandingCustomFieldAsync(input);
                if (response.ID <= 0) return Json(new AjaxRequestDTO
                {
                    Status = false,
                    Message = "Failed to Save Branding Custom Field",
                    Data = response
                });

                var now = ServerConfig.DateTimeNow;

                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Message = "Branding Custom Field Saved Successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteCustomField(DeleteDiscountInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid Data" });

                DeleteRequestDTO requestDTO = new DeleteRequestDTO
                {
                    ID = input.ID,
                };
                if (await _brandingRepo.DeleteCustomFieldAsync(requestDTO))
                {
                var now = ServerConfig.DateTimeNow;


                    return Json(new AjaxRequestDTO { Status = true, Message = "Custom Field Deleted successfully", });
                }


                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Delete Custom Field due to technical difficulties" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO());
            }
        }

        private bool IsBase64String(string base64)
        {
            return !string.IsNullOrWhiteSpace(base64) && !base64.StartsWith(ServerConfig.BlobPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}