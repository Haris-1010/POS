using CoreMedixBeta.Models.Brandings.DTOs;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using CoreMedixBeta.Models.PurchaseOrders;
using CoreMedixBeta.Models.PurchaseOrders.DTOs;
using CoreMedixBeta.Models.Purchases;
using CoreMedixBeta.Models.Purchases.DTOs;
using CoreMedixBeta.Models.Sales;
using CoreMedixBeta.Models.Sales.DTOs;
using CoreMedixBeta.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class PrintController : Controller
    {
        private readonly BrandingsRepository _brandingsRepository;
        private readonly PurchaseOrdersRepository _purchaseOrdersRepository;
        private readonly ItemAttributesRepository _itemAttributesRepository;
        private readonly SalesRepository _salesRepository;
        private readonly PurchasesRepository _purchasesRepository;

        public PrintController() : this(new BrandingsRepository(),
            new PurchaseOrdersRepository(), new ItemAttributesRepository(),
            new SalesRepository(), new PurchasesRepository())
        {
        }

        public PrintController(
            BrandingsRepository brandingsRepository,
            PurchaseOrdersRepository purchaseOrdersRepository,
            ItemAttributesRepository itemAttributesRepository,
            SalesRepository salesRepository,
            PurchasesRepository purchasesRepository
            )
        {
            _brandingsRepository = brandingsRepository;
            _purchaseOrdersRepository = purchaseOrdersRepository;
            _itemAttributesRepository = itemAttributesRepository;
            _salesRepository = salesRepository;
            _purchasesRepository = purchasesRepository
;
        }

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> PrintSaleInvoiceThermal(int ID)
        {
            try
            {
                if (ID <= 0) return View("SaleInvoices", "Sales");
                var viewModel = new PrintSaleInvoiceVM
                {
                    CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                    {
                    }),
                    InvoiceDetail = await _salesRepository.GetInvoiceDetailForPrint(new GetSaleInvoiceInputDto()
                    {
                        InvoiceID = ID,
                    }),
                    Branding = await _brandingsRepository.GetPrintBrandingAsync(new GetPrintBrandingInputDto
                    {
                    })
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return View(new PrintSaleInvoiceVM());
            }
        }

        public async Task<ActionResult> PrintSaleInvoiceThermalV2(int ID)
        {
            try
            {
                if (ID <= 0) return View("SaleInvoices", "Sales");

                var viewModel = new PrintSaleInvoiceVM
                {
                    CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                    {
                    }),
                    InvoiceDetail = await _salesRepository.GetInvoiceDetailForPrint(new GetSaleInvoiceInputDto()
                    {
                        InvoiceID = ID,
                    }),
                    Branding = await _brandingsRepository.GetPrintBrandingAsync(new GetPrintBrandingInputDto
                    {
                    }),
                    PrintBrandingCustomFieldsList = await _brandingsRepository.GetPrintBrandingCustomFieldShortListAsync(new RequestByMasterUsernameDTO
                    {
                    })
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return View(new PrintSaleInvoiceVM());
            }
        }

        public async Task<ActionResult> PrintPurchaseOrder(int ID)
        {
            if (ID <= 0) return View("PurchaseOrders", "Purchases");

            var viewModel = new PrintPurchaseOrderVM
            {
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                }),
                DetailedOrder = await _purchaseOrdersRepository.GetDetailedPurchaseOrder(new GetDetailedPurchaseOrderInputDto
                {
                    ID = ID,
                }),
                Branding = await _brandingsRepository.GetPrintBrandingAsync(new GetPrintBrandingInputDto
                {
                })
            };

            return View(viewModel);
        }

        public async Task<ActionResult> PrintPurchase(int ID)
        {
            if (ID <= 0) return View("PurchaseOrders", "Purchases");

            var viewModel = new PrintPurchaseVM
            {
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                }),
                Purchase = await _purchasesRepository.GetDetailedPurchases(new GetDetailedPurchasesInputDto
                {
                    ID = ID,
                }),
                Branding = await _brandingsRepository.GetPrintBrandingAsync(new GetPrintBrandingInputDto
                {
                }) 
            };

            return View(viewModel);
        }
    }
}