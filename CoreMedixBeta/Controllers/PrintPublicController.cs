//using CoreMedixBeta.Models.Brandings.DTOs;
//using CoreMedixBeta.Models.InventorySettings.DTOs;
//using CoreMedixBeta.Models.PurchaseOrders.DTOs;
//using CoreMedixBeta.Repositories;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;

//namespace CoreMedixBeta.Controllers
//{
//    public class PrintPublicController : Controller
//    {
//        // private readonly ISalesRepository _salesRepository;
//        private readonly PurchaseOrdersRepository _purchaseOrdersRepository;

//        private readonly ItemAttributesRepository _itemAttributesRepository;
//        private readonly BrandingsRepository _pharmacyBrandingRepo;

//        public async Task<ActionResult> PrintSaleInvoice(int ID)
//        {
//            try
//            {
//                if (ID <= 0) return View("SaleInvoices", "Sales");
//                var invoiceDetail = await _salesRepository.GetInvoiceDetailForPrint(new GetSaleInvoiceInputDto()
//                {
//                    InvoiceID = ID,
//                    IsPrint = true
//                });

//                var branding = await _pharmacyBrandingRepo.GetPrintBrandingAsync(new GetPrintBrandingInputDto
//                {
//                });

//                var currencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
//                {
//                });
//                var viewModel = new PrintSaleInvoiceVM
//                {
//                    InvoiceDetail = invoiceDetail,
//                    Branding = branding,
//                    CurrencySymbol = currencySymbol
//                };
//                return View(viewModel);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//                return View(new PrintSaleInvoiceVM());
//            }
//        }

//        public async Task<ActionResult> PrintPurchaseOrder(int id)
//        {
//            try
//            {
//                if (id <= 0) return View("PurchaseOrders", "Purchases");

//                var detailedOrder = await _purchaseOrdersRepository.GetDetailedPurchaseOrder(new GetDetailedPurchaseOrderInputDto
//                {
//                    ID = id,
//                    IsPrint = true
//                });

//                var branding = await _pharmacyBrandingRepo.GetPrintBrandingAsync(new GetPrintBrandingInputDto
//                {
//                });

//                var currencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
//                {
//                });

//                var viewModel = new PrintPurchaseOrderVM
//                {
//                    CurrencySymbol = currencySymbol,
//                    Branding = branding,
//                    DetailedOrder = detailedOrder,
//                };

//                return View(viewModel);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//                return View(new PrintPurchaseOrderVM());
//            }
//        }
//    }
//}