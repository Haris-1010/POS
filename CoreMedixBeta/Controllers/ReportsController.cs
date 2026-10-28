using AutoMapper;
using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using CoreMedixBeta.Models.ManufacturerBrand;
using CoreMedixBeta.Models.Reports;
using CoreMedixBeta.Models.Reports.DTOs;
using CoreMedixBeta.Models.Supplier.DTOs;
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
    public class ReportsController : Controller
    {
        private readonly ReportsRepository _reportsRepository;
        private readonly ManufacturerRepository _manufacturersRepository;
        private readonly ManufacturerBrandsRepository _brandsRepository;
        private readonly SupplierRepository _supplierRepository;
        private readonly ItemAttributesRepository _itemAttributesRepository;
        private readonly DashBoardRepository _dashBoardRepo;
        private readonly DiscountsRepository _discountsRepository;
        private readonly ActivityLogRepository _activityLogRepo = new ActivityLogRepository();

        public ReportsController() : this(new ReportsRepository(), new ManufacturerRepository(),
            new ManufacturerBrandsRepository(), new SupplierRepository(),
            new ItemAttributesRepository(), new DashBoardRepository(), new DiscountsRepository())
        {
        }

        public ReportsController(
            ReportsRepository reports,
            ManufacturerRepository manufacturerRepository,
            ManufacturerBrandsRepository manufacturerBrands,
            SupplierRepository supplier,
            ItemAttributesRepository itemAttributes,
            DashBoardRepository dashBoard,
            DiscountsRepository discountsRepository
            )
        {
            _reportsRepository = reports;
            _manufacturersRepository = manufacturerRepository;
            _brandsRepository = manufacturerBrands;
            _supplierRepository = supplier;
            _itemAttributesRepository = itemAttributes;
            _dashBoardRepo = dashBoard;
            _discountsRepository = discountsRepository;
        }

        [AuthorizePermission("Reports_View")]
        public ActionResult ReportsGroup() => View();

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> SaleReport(string StartDate, string EndDate)
        {
            var model = new SaleReportVM
            {
                StartDate = StartDate,
                EndDate = EndDate,
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto { }),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto { })
            };

            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedSaleReport(GetPagedSaleReportInputDto input)
        {
            try
            {
                if (input.DataTableValues == null)
                {
                    input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                }
                var data = await _reportsRepository.GetPagedSaleReport(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> DailySalesReport()
        {
            return View(new DailySaleReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedDailySalesReport(GetPagedDailySalesReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedDailySalesReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> SalesSummaryReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new SalesSummaryReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                }),
                Discounts = await _discountsRepository.GetDiscountsShortListAsync(new RequestByMasterUsernameDTO
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedSalesSummaryReport(GetPagedSalesSummaryReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedSalesSummaryReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> SalesReturnReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });

            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new SalesReturnReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedSalesReturnReport(GetPagedSalesReturnReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedSalesReturnReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> CustomerLedger()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedCustomerLedger(GetPagedCustomerLedgerInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedCustomerLedgerReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> TopSellingProducts()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new TopSellingProductsVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedTopSellingProducts(GetPagedTopSellingProductsInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedTopSellingProductsAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> LowStockReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new LowStockReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetLowStockReport(GetPagedLowStockReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedLowStockReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> ExpiryReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new ExpiryReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),

                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetExpiryReport(GetPagedExpiryReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedExpiryReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> PurchaseReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new PurchaseReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPurchaseReport(GetPagedPurchaseReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedPurchaseReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> PurchaseReturnReport()
        {
            return View(new PurchaseReturnReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> StockValuationReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new StockValuationReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetStockValuationReport(GetPagedStockValuationReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedStockValuationReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> InventoryMovementReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new ExpiryReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),

                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetInventoryMovementReport(GetPagedInventoryMovementReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedInventoryMovementReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> SupplierLedgerReport()
        {
            return View(new SupplierLedgerVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetSupplierLedgerReport(GetPagedSupplierLedgerReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedSupplierLedgerReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> ProfitMarginReport(string StartDate, string EndDate)
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new ProfitMarginReportVM
            {
                StartDate = StartDate,
                EndDate = EndDate,
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetProfitMarginReport(GetPagedProfitMarginReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedProfitMarginReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> BatchWiseStockReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new BatchWiseStockReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),

                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetBatchWiseStockReport(GetPagedBatchWiseStockReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedBatchWiseStockReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> UserActivityReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new TaxReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetUserActivityReport(GetPagedUserActivityReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetUserActivityReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> TaxReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });

            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new TaxReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetTaxReport(GetPagedTaxReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedTaxReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> DeletedInvoicesReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });

            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new DeletedInvoicesReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetDeletedInvoicesReport(GetPagedDeletedInvoicesReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedDeletedInvoicesReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> AuditLogsReport()
        {
            ViewBag.Actions = await _activityLogRepo.GetDistinctActionsAsync();
            ViewBag.Usernames = await _activityLogRepo.GetDistinctUsernamesAsync();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetAuditLogsReport(GetPagedAuditLogsInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _activityLogRepo.GetPagedAuditLogsAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> CustomerOutstandingReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetCustomerOutstandingReport(GetPagedCustomerOutstandingReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedCustomerOutstandingReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> SupplierOutstandingReport()
        {
            return View(new SupplierOutstandingReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetSupplierOutstandingReport(GetPagedSupplierOutstandingReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedSupplierOutstandingReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> IncomeReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });

            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new DeletedInvoicesReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetIncomeReport(GetPagedIncomReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedIncomeReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> SlowMovingItemsReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new ProfitMarginReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetSlowMovingItemsReport(GetPagedProductWiseProfitReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedSlowMovingItemsReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> ManualStockAdjustment()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new DeletedInvoicesReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetManualStockAdjustmentReport(GetPagedManualAdjustmentReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedManualStockAdjustmentReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> CashCollectionReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new DeletedInvoicesReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetCashCollectionReport(GetPagedCashCollectionReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedCashCollectionReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> DailyActivityReport()
        {
            ViewBag.CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
            {
            });
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new TaxReportVM
            {
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetDailyActivityReport(GetPagedDailyActivityReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetDailyActivityReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> ManufacturerWiseSalesReport()
        {
            return View(new ManufacturerWiseSalesReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetManufacturerWiseSalesReport(GetPagedManufacturerWiseSalesReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedManufacturerWiseSalesReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> ProductWiseProfitReport()
        {
            GetAllAttributesInputDto input = new GetAllAttributesInputDto
            {
            };
            return View(new ProfitMarginReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                AllAttributes = await _itemAttributesRepository.GetAllAttributesAsync(input),

                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetProductWiseProfitReport(GetPagedProductWiseProfitReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedProductWiseProfitReportAsync(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> ItemWiseSaleReport()
        {
            return View(new SaleReportVM
            {
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedItemWiseSaleReport(GetPagedSaleReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedItemWiseSaleReport(input);

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

        [AuthorizePermission("Reports_View")]
        public async Task<ActionResult> StockInHandReport()
        {
            var dbCard = await _dashBoardRepo.GetDashBoardCardsAsync(new RequestByMasterUsernameDTO
            {
            });
            ViewBag.TotalStockValue = Math.Round(dbCard.TotalStockValue, 0, MidpointRounding.AwayFromZero).ToString("N0");
            ViewBag.TotalStockQuantity = dbCard.TotalStock.ToString("N0");

            return View(new StockInHandReportVM
            {
                Manufacturers = await _manufacturersRepository.GetManufacturerShortList(),
                Suppliers = await _supplierRepository.GetSupplierShortList(),
                Brands = await _brandsRepository.GetBrandsByMasterUsernameShortList(new GetBrandShortListInputDto
                {
                }),
                CurrencySymbol = await _itemAttributesRepository.GetCurrencyAsync(new GetPreferenceInputDto
                {
                })
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetPagedStockInHandReport(GetPagedStockInHandReportInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _reportsRepository.GetPagedStockInHandReport(input);
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
    }
}