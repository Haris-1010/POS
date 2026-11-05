using HARTPOS.Enums;
using HARTPOS.Models.Patient;
using HARTPOS.Models.Reports.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace HARTPOS.Repositories
{
    public class ReportsRepository
    {
        public async Task<Tuple<string, string>> GetPagedItemWiseSaleReport(GetPagedSaleReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT I.Name ItemName,
                       COUNT(DISTINCT SI.CustomerID) AS TotalCustomers,
                       SUM(I.Qty) AS Qty,
                       MAX(I.UnitSalePriceInclusiveTax) AS PerUnitPrice,
                       SUM(I.LineTotal) AS LineTotal
                       FROM Sale.SaleInvoiceItems I
                       INNER JOIN Sale.SaleInvoices SI ON I.InvoiceID = SI.ID
                       INNER JOIN Inventory.InventoryItems INI ON INI.ID = I.ItemID
                       WHERE  SI.IsDeleted = 0 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND INI.SupplierID = @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND INI.ManufacturerID = @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND INI.BrandID = @BrandID ";
                }

                if (!string.IsNullOrWhiteSpace(input.ReferredByID) && !string.IsNullOrWhiteSpace(input.ReferredByType))
                {
                    dbValues.Add("@ReferredByID", input.ReferredByID);
                    dbValues.Add("@ReferredByType", input.ReferredByType);
                    query += " AND (SI.ReferredByID = @ReferredByID AND SI.ReferredByType = @ReferredByType) ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND I.Name LIKE @SearchString  ";
                }

                query += @" GROUP BY I.Name ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ItemName" : input.SortBy)}",
                                 string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedItemWiseSaleReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedSaleReport(GetPagedSaleReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT
                            SI.ID,
                            I.Name AS ItemName,
                            I.Qty,
                            I.UnitSalePriceInclusiveTax AS RatePerUnit,
                            I.LineTotal,
                            ISNULL(M.Name, 'No Manufacturer') AS ManufacturerName,
                            FORMAT(SI.InvoiceDate, 'dd MMM yyyy') AS DateTime,
                            (SELECT C.Name
                                FROM Patient.Patients C
                                WHERE C.ID = SI.CustomerID
                            ) AS CustomerName
                        FROM Sale.SaleInvoiceItems I
                        INNER JOIN Sale.SaleInvoices SI ON I.InvoiceID = SI.ID
                        INNER JOIN Inventory.InventoryItems INI ON INI.ID = I.ItemID
                        LEFT JOIN Manufacturer.Manufacturers M ON M.ID = INI.ManufacturerID
                        --LEFT JOIN Manufacturer.ManufacturerBrands B ON B.ManufacturerID = M.ID
                        WHERE SI.IsDeleted = 0

                         ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND INI.SupplierID = @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND INI.ManufacturerID = @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND INI.BrandID = @BrandID ";
                }

                if (!string.IsNullOrWhiteSpace(input.ReferredByID) && !string.IsNullOrWhiteSpace(input.ReferredByType))
                {
                    dbValues.Add("@ReferredByID", input.ReferredByID);
                    dbValues.Add("@ReferredByType", input.ReferredByType);
                    query += " AND (SI.ReferredByID = @ReferredByID AND SI.ReferredByType = @ReferredByType) ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND I.Name LIKE @SearchString  ";
                }
                int p = 0;
                 
                p = dataTableValues.Offset / dataTableValues.PageSize;
               
                 Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(input.GroupByColumn))
                {
                    sortAndOrder.Add($"MyList.{input.GroupByColumn}", "DESC");
                }
                else
                {
                    sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);
                }

                var temp = await DBHelper.GetPagedListAsync<GetPagedSaleReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                string jr = JsonConvert.SerializeObject(temp.Results);
                string total = JsonConvert.SerializeObject(temp.pager.TotalRecords);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedStockInHandReport(GetPagedStockInHandReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT I.ID, CONCAT(I.Name,' - ',V.VariationName) ItemName, 
                       V.SKU, V.SellingPrice SalePrice, V.CostPrice PurchasePrice,V.OpeningStock Qty,
                       C.Title AS Category, U.Title AS Unit, V.NoOfPillsInPack AS PackUnits
                       FROM Inventory.InventoryItems I
                       INNER JOIN Inventory.InventoryItemVariations V ON V.ItemID = I.ID
                       LEFT JOIN Inventory.ItemAttributes C ON C.ID = V.CategoryID AND C.AttributeType = 'Category'
                       LEFT JOIN Inventory.ItemAttributes U ON U.ID = V.WeightUnitID AND U.AttributeType = 'WeightUnit'
                       WHERE  I.IsDeleted = 0 ";

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND (I.SupplierID = @SupplierID OR @SupplierID IN (SELECT value FROM STRING_SPLIT(I.SupplierID, ','))) ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND I.BrandID = @BrandID ";
                }

                if (input.QtyFilter != QtyFilter.None)
                {
                    dbValues.Add("@QtyValue", input.QtyValue.ToString());
                    switch (input.QtyFilter)
                    {
                        case QtyFilter.GreaterThan:
                            query += " AND V.OpeningStock > @QtyValue";
                            break;

                        case QtyFilter.EqualTo:
                            query += " AND V.OpeningStock = @QtyValue";
                            break;

                        case QtyFilter.GreaterThanOrEqualTo:
                            query += " AND V.OpeningStock >= @QtyValue";
                            break;

                        case QtyFilter.LessThan:
                            query += " AND V.OpeningStock < @QtyValue";
                            break;

                        case QtyFilter.LessThanOrEqualTo:
                            query += " AND V.OpeningStock <= @QtyValue";
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,I.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (I.Name LIKE @SearchString OR V.VariationName LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(input.GroupByColumn))
                {
                    sortAndOrder.Add($"MyList.{input.GroupByColumn}", "DESC");
                }
                else
                {
                    sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ItemName" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);
                }
                var temp = await DBHelper.GetPagedListAsync<GetPagedStockInHandReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedDailySalesReportAsync(GetPagedDailySalesReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>();

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"
    SELECT
        SI.InvoiceNo,
        SI.CreatedOn AS DateTime,
        (SELECT COUNT(*) FROM Sale.SaleInvoiceItems X WHERE X.InvoiceID = SI.ID) AS ItemsCount,
        (SELECT ISNULL(SUM(X.Qty), 0) FROM Sale.SaleInvoiceItems X WHERE X.InvoiceID = SI.ID) AS TotalQty,
        ISNULL(C.Name, 'Walk-in Customer') AS CustomerName,
        FD.SubTotal AS GrossAmount,
        FD.DiscountAmount AS Discount,
        FD.SubtotalAfterTax AS NetAmount
    FROM Sale.SaleInvoices AS SI
    LEFT JOIN Sale.SaleInvoiceFinancialDetails AS FD ON FD.InvoiceID = SI.ID
    LEFT JOIN Patient.Patients C ON C.ID = SI.CustomerID
    WHERE SI.IsDeleted = 0 ";

                #region Date Filters
                if (!string.IsNullOrEmpty(input.StartDate))
                {
                    DateTime startDate = Convert.ToDateTime(input.StartDate).Date;
                    DateTime nextDay = startDate.AddDays(1);

                    dbValues.Add("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    dbValues.Add("@NextDay", nextDay.ToString("yyyy-MM-dd HH:mm:ss"));

                    query += " AND SI.CreatedOn >= @StartDate AND SI.CreatedOn < @NextDay ";
                }
                else
                {
                    DateTime today = DateTime.Today;
                    DateTime tomorrow = today.AddDays(1);

                    dbValues.Add("@StartDate", today.ToString("yyyy-MM-dd HH:mm:ss"));
                    dbValues.Add("@NextDay", tomorrow.ToString("yyyy-MM-dd HH:mm:ss"));

                    query += " AND SI.CreatedOn >= @StartDate AND SI.CreatedOn < @NextDay ";
                }
                #endregion

                #region Optional Filters
                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND EXISTS (SELECT 1 FROM Sale.SaleInvoiceItems X INNER JOIN Inventory.InventoryItems INI ON INI.ID = X.ItemID WHERE X.InvoiceID = SI.ID AND INI.SupplierID = @SupplierID) ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND EXISTS (SELECT 1 FROM Sale.SaleInvoiceItems X INNER JOIN Inventory.InventoryItems INI ON INI.ID = X.ItemID WHERE X.InvoiceID = SI.ID AND INI.ManufacturerID = @ManufacturerID) ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND EXISTS (SELECT 1 FROM Sale.SaleInvoiceItems X INNER JOIN Inventory.InventoryItems INI ON INI.ID = X.ItemID WHERE X.InvoiceID = SI.ID AND INI.BrandID = @BrandID) ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND SI.SalePersonUsername = @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(input.PaymentMode))
                {
                    dbValues.Add("@PaymentMode", input.PaymentMode);
                    query += " AND EXISTS (SELECT 1 FROM Sale.SaleInvoicePayments SIP WHERE SIP.InvoiceID = SI.ID AND SIP.PaymentMode = @PaymentMode) ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND SI.InvoiceNo LIKE @SearchString ";
                }
                #endregion



                 int pageIndex = dataTableValues.Offset / dataTableValues.PageSize;
                 Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                 {
                        { $"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "InvoiceNo" : input.SortBy)}",
                          string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder }
                 };

                // Execute
                var temp = await DBHelper.GetPagedListAsync<GetPagedDailySalesReportOutputDto>(
                    query,
                    sortAndOrder,
                    dataTableValues.PageSize,
                    pageIndex,
                    dbValues
                );

                return Tuple.Create(
                    JsonConvert.SerializeObject(temp.Results),
                    JsonConvert.SerializeObject(temp.pager.TotalRecords)
                );

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedSalesSummaryReportAsync(GetPagedSalesSummaryReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT SI.ID, ISNULL(IIV.VariationName, SII.Name) AS Name, IA.Title AS Category, SII.Qty, ISNULL(IIV.CostPrice, 0) AS PurchaseRate, SII.UnitCost AS SaleRate, SII.UnitCost * SII.Qty AS GrossAmount, SII.DiscountType,
                                    CASE
									WHEN SII.DiscountType = 'percentage' THEN
										 (SII.Discount * SII.UnitCost * SII.Qty) / 100
									ELSE
										 SII.Discount * SII.Qty
									END	AS Discount,
                                    CASE
									WHEN SII.DiscountType = 'percentage' THEN
										(SII.UnitCost * SII.Qty) - ((SII.UnitCost * SII.Qty) * (SII.Discount / 100.0))
									ELSE
										(SII.UnitCost * SII.Qty) - (SII.Discount * SII.Qty)
								    END AS NetSale,
                                    CASE
                                    WHEN IIV.ID IS NULL THEN 0
                                    WHEN SII.DiscountType = 'percentage' THEN
                                        ((SII.UnitCost - IIV.CostPrice) * SII.Qty)
										- ((SII.UnitCost * SII.Qty) * (SII.Discount / 100.0))
									ELSE
										((SII.UnitCost - IIV.CostPrice) * SII.Qty)
										- SII.Discount * SII.Qty
								    END AS Profit
                                    FROM Sale.SaleInvoices AS SI
                                    INNER JOIN Sale.SaleInvoiceItems AS SII ON SII.InvoiceID = SI.ID
                                    LEFT JOIN Inventory.InventoryItemVariations AS IIV ON IIV.ID = SII.VariationID
                                    INNER JOIN Inventory.InventoryItems AS II ON II.ID = SII.ItemID
                                    LEFT JOIN Inventory.ItemAttributes AS IA ON IA.ID = IIV.CategoryID
                                    WHERE  SI.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND SI.SalePersonUsername = @SalePersonUsername";
                }
                if (input.DiscountID > 0)
                {
                    dbValues.Add("@DiscountID", input.DiscountID.ToString());
                    query += " AND SII.DiscountID =  @DiscountID ";
                }
                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }
                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }
                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IA.ID = @CategoryID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (ISNULL(IIV.VariationName, SII.Name) LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedSalesSummaryReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedSalesReturnReportAsync(GetPagedSalesReturnReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT IH.ID, SI.InvoiceNo, IH.TimeStamp AS [DateTime],
                                        ISNULL(SII.Name, I.Name) AS Product, IH.Qty AS QtyReturned,
                                        CAST(IH.Qty * (CASE WHEN SII.Qty > 0 THEN SII.LineTotal / SII.Qty ELSE 0 END)
                                             * (CASE WHEN ISNULL(SFD.SubTotal, 0) > 0 THEN SFD.SubtotalAfterTax / SFD.SubTotal ELSE 1 END)
                                             AS DECIMAL(18, 2)) AS ReturnAmount,
                                        ISNULL((SELECT C.Name FROM Patient.Patients C WHERE C.ID = SI.CustomerID), 'Walk-in Customer') AS Customer,
                                        ISNULL(NULLIF(U.FullName, ''), ISNULL(NULLIF(IH.AddedBy, ''), ISNULL(NULLIF(SI.LastModifiedBy, ''), ''))) AS [User]
                                 FROM Inventory.InventoryHistories AS IH
                                 INNER JOIN Sale.SaleInvoices AS SI ON SI.ID = IH.SaleInvoiceID
                                 INNER JOIN Inventory.InventoryItems I ON I.ID = IH.ItemID
                                 LEFT JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = SI.ID
                                 OUTER APPLY (SELECT TOP 1 X.Name, X.Qty, X.LineTotal FROM Sale.SaleInvoiceItems X
                                              WHERE X.InvoiceID = SI.ID AND X.VariationID = IH.VariationID) SII
                                 LEFT JOIN Auth.Users U ON U.Username = ISNULL(NULLIF(IH.AddedBy, ''), SI.LastModifiedBy)
                                 WHERE IH.Soruce = 'Sale Return' AND IH.Qty > 0 AND SI.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,IH.TimeStamp) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND SI.SalePersonUsername = @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    dbValues.Add("@CustomerType", input.CustomerType);
                    query += " AND SI.CustomerID =  @CustomerID AND SI.CustomerType = @CustomerType ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND I.ID = @ItemID ";
                }
                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND I.SupplierID = @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND I.BrandID = @BrandID ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (I.Name LIKE @SearchString OR SI.InvoiceNo LIKE @SearchString ";
                    query += " OR ISNULL(U.FullName, '') LIKE @SearchString ";
                    query += " OR (SELECT C.Name FROM Patient.Patients C WHERE C.ID = SI.CustomerID) LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "InvoiceNo" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedSalesReturnReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedCustomerLedgerReportAsync(GetPagedCustomerLedgerInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT E.CustomerID, E.DateTime, E.InvoiceNo, E.Description, E.Debit, E.Credit,
                                ISNULL(( SELECT C.Name
                                          FROM Patient.Patients C
                                          WHERE C.ID = e.CustomerID), 'Walk-in Customer') AS CustomerName,
                                SUM(E.Debit - E.Credit) OVER (
                                    PARTITION BY E.CustomerID
                                    ORDER BY E.DateTime, E.InvoiceNo, E.SortOrder
                                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                                ) AS [RunningBalance]
                            FROM (
                                -- 1. Sale Invoice (Debit)
                                SELECT
                                    SI.CustomerID,
                                    SI.CustomerType,
                                    SI.CreatedOn AS [DateTime],
                                    SI.InvoiceNo,
                                    'Sale Invoice' AS Description,
                                    SFD.SubtotalAfterTax AS Debit,
                                    0.00 AS Credit,  
                                    1 AS SortOrder
                                FROM Sale.SaleInvoices SI
                                INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SI.ID = SFD.InvoiceID
                                WHERE SI.IsDeleted = 0

                                UNION ALL

                                -- 2. Payments (Credit)
                                SELECT
                                    SI.CustomerID,
                                    SI.CustomerType,
                                    SI.CreatedOn AS [DateTime],
                                    SI.InvoiceNo,
                                    'Payment - ' + ISNULL(SIP.PaymentMode, '') AS Description,
                                    0.00 AS Debit,
                                    SIP.PaidAmount AS Credit, 
                                    2 AS SortOrder
                                FROM Sale.SaleInvoicePayments SIP
                                INNER JOIN Sale.SaleInvoices SI ON SI.ID = SIP.InvoiceID
                                WHERE SI.IsDeleted = 0
                                AND SIP.TransactionType = 'Payment'

                                UNION ALL

                                -- 3. Returns (SHOULD BE DEBIT — it's a refund, not a payment)
                                SELECT
                                    SI.CustomerID,
                                    SI.CustomerType,
                                    SI.CreatedOn AS [DateTime],
                                    SI.InvoiceNo,
                                    'Refund - ' + ISNULL(SIP.PaymentMode, '') AS Description,
                                    SIP.PaidAmount AS Debit,
                                    0.00 AS Credit, 
                                    3 AS SortOrder
                                FROM Sale.SaleInvoicePayments SIP
                                INNER JOIN Sale.SaleInvoices SI ON SI.ID = SIP.InvoiceID
                                WHERE SI.IsDeleted = 0
                                AND SIP.TransactionType = 'Return'

                                UNION ALL

                                -- 4. Goods returned (Credit: reduces what the customer owes)
                                SELECT
                                    SI.CustomerID,
                                    SI.CustomerType,
                                    ISNULL(SI.LastModifiedOn, SI.CreatedOn) AS [DateTime],
                                    SI.InvoiceNo,
                                    'Sale Return' AS Description,
                                    0.00 AS Debit,
                                    SFD.ReturnAmount AS Credit,
                                    4 AS SortOrder
                                FROM Sale.SaleInvoices SI
                                INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SI.ID = SFD.InvoiceID
                                WHERE SI.IsDeleted = 0
                                AND ISNULL(SFD.ReturnAmount, 0) > 0
                                ) AS E
                                WHERE 1=1 ";

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    query += " AND E.CustomerID = @CustomerID ";
                }
                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,E.DateTime) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (E.InvoiceNo LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "InvoiceNo" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedCustomerLedgerOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedTopSellingProductsAsync(GetPagedTopSellingProductsInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT II.Name AS [ProductName], IAA.Title AS Varient, IA.Title AS [Category], ISNULL(M.Name, 'No Manufacturer') AS Manufacturer,
                                SUM((SII.Qty - ISNULL(SII.RetQty, 0))) AS [QtySold], SUM((CASE WHEN SII.Qty > 0 THEN SII.LineTotal * (SII.Qty - ISNULL(SII.RetQty, 0)) / SII.Qty ELSE 0 END)) AS [TotalSale], COUNT(DISTINCT SI.ID) AS [SaleCount]
                                FROM Sale.SaleInvoiceItems SII
                                INNER JOIN Sale.SaleInvoices SI ON SI.ID = SII.InvoiceID AND SI.IsDeleted = 0
                                INNER JOIN Inventory.InventoryItems II ON II.ID = SII.ItemID
                                LEFT JOIN Inventory.InventoryItemVariations IV ON IV.ID = SII.VariationID
                                LEFT JOIN Inventory.ItemAttributes IA ON IV.CategoryID = IA.ID
                                LEFT JOIN Inventory.ItemAttributes IAA ON IV.FormationID = IAA.ID
                                LEFT JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
                                WHERE (SII.Qty - ISNULL(SII.RetQty, 0)) > 0 AND SI.IsDeleted = 0 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND II.BrandID =  @BrandID ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND IV.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IA.ID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAA.ID = @VarientID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (II.Name LIKE @SearchString) ";
                }

                query += @" GROUP BY II.Name, IA.Title,M.Name, IAA.Title ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "QtySold" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedTopSellingProductsOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedLowStockReportAsync(GetPagedLowStockReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT IV.ID, IV.VariationName AS ProductName, IAV.Title AS Varient, IAC.Title AS Category, ISNULL(M.Name, 'No Manufacturer') AS Manufacturer,
                                IV.OpeningStock AS LatestQty, IV.ReorderPoint, IAU.Title AS Unit, II.ID AS ItemID,
                                (
                                 SELECT STRING_AGG(S.Name, ', ')
                                    FROM Supplier.Suppliers S
                                    WHERE ',' + CAST(II.SupplierID AS NVARCHAR(MAX))  + ',' LIKE '%,' +CAST(S.ID AS NVARCHAR)+ ',%' 
                                ) AS Supplier
                                FROM Inventory.InventoryItems AS II
                                INNER JOIN Inventory.InventoryItemVariations AS IV ON II.ID = IV.ItemID
                                --INNER JOIN Inventory.InventoryHistories AS IH ON II.ID = IH.ItemID
                                LEFT JOIN Inventory.ItemAttributes AS IAC ON IV.CategoryID = IAC.ID AND IAC.AttributeType = 'Category' 
                                LEFT JOIN Inventory.ItemAttributes AS IAV ON IV.FormationID = IAV.ID AND IAV.AttributeType = 'Formation'  
                                LEFT JOIN Inventory.ItemAttributes AS IAU ON IV.WeightUnitID = IAU.ID AND IAU.AttributeType = 'WeightUnit'  
                                LEFT JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
                                WHERE II.IsDeleted = 0 ";

                // Low stock = at or below the item's reorder point, or at or below the
                // "Minimum Qty" typed in the filter (it used to match that number exactly).
                if (input.MinimumQty > 0)
                {
                    dbValues.Add("@MinimumQty", input.MinimumQty.ToString());
                    query += " AND IV.OpeningStock <= @MinimumQty ";
                }
                else
                {
                    query += " AND IV.OpeningStock <= IV.ReorderPoint ";
                }
                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }
                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND IV.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IAC.ID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAV.ID = @VarientID ";
                }

                // Low stock is about current stock, so there is no date filter.

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (IV.VariationName LIKE @SearchString) ";
                }
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedLowStockReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedExpiryReportAsync(GetPagedExpiryReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT 
                                    IV.VariationName AS ProductName, 
                                    IAV.Title AS Varient, 
                                    IAC.Title AS Category,
                                    IV.Batch,  
                                    IV.ExpiryDate, 
                                    IV.OpeningStock AS Qty, 
                                    ISNULL(M.Name, 'No Manufacturer') AS Manufacturer, 
                                    II.ID AS ItemID,
                                    (
                                        SELECT STRING_AGG(S.Name, ', ')
                                        FROM Supplier.Suppliers S
                                        WHERE ',' + CAST(II.SupplierID AS NVARCHAR(MAX)) + ',' LIKE '%,' + CAST(S.ID AS NVARCHAR) + ',%'
                                    ) AS Supplier
                                FROM Inventory.InventoryItems AS II
                                INNER JOIN Inventory.InventoryItemVariations AS IV ON II.ID = IV.ItemID
                                LEFT JOIN Inventory.ItemAttributes AS IAC ON IV.CategoryID = IAC.ID AND IAC.AttributeType = 'Category' 
                                LEFT JOIN Inventory.ItemAttributes AS IAV ON IV.FormationID = IAV.ID AND IAV.AttributeType = 'Formation'  
                                LEFT JOIN Inventory.ItemAttributes AS IAU ON IV.WeightUnitID = IAU.ID AND IAU.AttributeType = 'WeightUnit'  
                                LEFT JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
                                WHERE II.IsDeleted = 0 AND IV.OpeningStock > 0 ";

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND IV.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IAC.ID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAV.ID = @VarientID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,IV.ExpiryDate) BETWEEN @StartDate AND @EndDate ";
                }
                else
                {
                    query += " AND IV.ExpiryDate < CAST(GETDATE() AS DATE) ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (IV.VariationName LIKE @SearchString) ";
                }
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ProductName" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedExpiryReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedPurchaseReportAsync(GetPagedPurchaseReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT DISTINCT PO.ID, PO.ReferenceNo AS InvoiceNo, PO.OrderDate, P.PurchaseReceivedAt, PO.Status, S.Name AS Supplier,
                                    POI.Name AS ProductName, IV.Batch, POI.Qty, POI.UnitCost, P.DiscountAmount, POI.LineTotal
                                 FROM Purchase.PurchaseOrders AS PO
                                 INNER JOIN Purchase.PurchaseOrderItems AS POI ON PO.ID = POI.PurchaseOrderID
                                 LEFT JOIN Purchase.Purchases AS P ON PO.ID = P.PurchaseOrderID
                                 LEFT JOIN Purchase.PurchaseItems AS PI ON P.ID = PI.PurchaseID
                                 LEFT JOIN Purchase.PurchasePayments AS PP ON P.ID = PP.PurchaseID
                                 LEFT JOIN Supplier.Suppliers AS S ON PO.SupplierID = S.ID
                                 INNER JOIN Inventory.InventoryItemVariations AS IV ON POI.VariationID = IV.ID
                                 INNER JOIN Inventory.InventoryItems I ON I.ID = POI.ItemID
                                 WHERE   PO.IsDeleted = 0 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND PO.SupplierID =  @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID =  @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND PO.BrandID =  @BrandID ";
                }
                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND IV.ID =  @VariationID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,PO.OrderDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (PO.ReferenceNo LIKE @SearchString OR POI.Name LIKE @SearchString OR S.Name LIKE @SearchString) ";
                }
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedPurchaseReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedStockValuationReportAsync(GetPagedStockValuationReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT II.ID, IIV.VariationName, IAV.Title AS Variant, IAC.Title AS Category, ISNULL(M.Name, 'No Manufacturer') AS Manufacturer,
                                 IIV.OpeningStock, IIV.CostPrice, (IIV.OpeningStock * IIV.CostPrice) AS TotalValue
                                 FROM Inventory.InventoryItems AS II
                                 INNER JOIN Inventory.InventoryItemVariations AS IIV ON II.ID = IIV.ItemID
                                 LEFT JOIN Inventory.ItemAttributes AS IAC ON IIV.CategoryID = IAC.ID AND IAC.AttributeType = 'Category'  
                                 LEFT JOIN Inventory.ItemAttributes AS IAV ON IIV.FormationID = IAV.ID AND IAV.AttributeType = 'Formation'  
                                 LEFT JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
                                 WHERE   II.IsDeleted = 0 ";

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND IIV.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IAC.ID = @CategoryID ";
                }

                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAV.ID = @VarientID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,II.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (IIV.VariationName LIKE @SearchString ) ";
                }
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedStockValuationReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedInventoryMovementReportAsync(GetPagedInventoryMovementReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT H.ID, V.VariationName AS ProductName, FORMAT(H.TimeStamp, 'dd MMM yyyy | hh:mm tt') TimeStamp,
							            H.Qty,  H.LatestQty AS Balance,
							    CASE
								WHEN SI.InvoiceNo IS NOT NULL THEN SI.InvoiceNo
								WHEN PO.ReferenceNo IS NOT NULL THEN PO.ReferenceNo
								ELSE NULL
							   END AS ReferenceNo,
                               H.OldQty, H.LatestQty, H.Soruce, H.Status +' - '+ H.Soruce AS TransactionType
                               FROM Inventory.InventoryHistories H
                               INNER JOIN Inventory.InventoryItems I ON H.ItemID = I.ID
                               INNER JOIN Inventory.InventoryItemVariations V ON V.ID = H.VariationID
							   LEFT JOIN Sale.SaleInvoices SI ON SI.ID = H.SaleInvoiceID
							   LEFT JOIN Purchase.PurchaseOrders PO ON PO.ID = H.PurchaseOrderID
                               WHERE I.IsDeleted = 0  ";

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID =  @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND I.SupplierID =  @SupplierID ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND I.ID =  @ItemID ";
                }
                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND V.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND V.CategoryID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND V.FormationID = @VarientID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,H.TimeStamp) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (V.VariationName LIKE @SearchString) ";
                }
                if (!string.IsNullOrEmpty(input.Status))
                {
                    dbValues.Add("@Status", input.Status);
                    query += " AND H.Status = @Status";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    {
                        $"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                        $"{(string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder)}"
                    }
                };

                var temp = await DBHelper.GetPagedListAsync<GetPagedInventoryMovementReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                string jr = JsonConvert.SerializeObject(temp.Results);
                string total = JsonConvert.SerializeObject(temp.pager.TotalRecords);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedSupplierLedgerReportAsync(GetPagedSupplierLedgerReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT T.*,
                                       SUM(T.Debit - T.Credit) OVER (PARTITION BY T.SupplierID ORDER BY T.DateTime, T.ID
                                                                     ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningBalance
                                FROM (
                                       SELECT PO.ID, PO.ReferenceNo, PO.OrderDate AS DateTime, PO.Status,
                                              SUM(P.SubtotalAfterTax) AS Debit, SUM(P.PaidAmount) AS Credit,
                                              S.Name AS SupplierName, MAX(P.AdditionalNotes) AS [Description], PO.SupplierID
                                       FROM Purchase.PurchaseOrders PO
                                       INNER JOIN Purchase.Purchases P ON P.PurchaseOrderID = PO.ID
                                       INNER JOIN Supplier.Suppliers S ON S.ID = PO.SupplierID
                                       WHERE PO.IsDeleted = 0
                                       GROUP BY PO.ID, PO.ReferenceNo, PO.OrderDate, PO.Status, S.Name, PO.SupplierID
                                ) AS T
                               WHERE 1=1";

                if (!string.IsNullOrEmpty(input.Status))
                {
                    dbValues.Add("@Status", input.Status);
                    query += " AND Status = @Status";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND SupplierID =  @SupplierID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,DateTime) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (ReferenceNo LIKE @SearchString OR SupplierName LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    {
                        $"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                        $"{(string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder)}"
                    }
                };

                var temp = await DBHelper.GetPagedListAsync<GetPagedSupplierLedgerReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                string jr = JsonConvert.SerializeObject(temp.Results);
                string total = JsonConvert.SerializeObject(temp.pager.TotalRecords);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedProfitMarginReportAsync(GetPagedProfitMarginReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT V.VariationName AS ProductName, IAF.Title AS Variant, IA.Title AS Category, V.CostPrice AS PurchaseRate,
                                 CAST(SUM((CASE WHEN SII.Qty > 0 THEN SII.LineTotal * (SII.Qty - ISNULL(SII.RetQty, 0)) / SII.Qty ELSE 0 END)) / NULLIF(SUM((SII.Qty - ISNULL(SII.RetQty, 0))), 0) AS DECIMAL(18, 2)) AS SaleRate,
                                 SUM((SII.Qty - ISNULL(SII.RetQty, 0))) AS QtySold,
                                 CAST(SUM((CASE WHEN SII.Qty > 0 THEN SII.LineTotal * (SII.Qty - ISNULL(SII.RetQty, 0)) / SII.Qty ELSE 0 END)) / NULLIF(SUM((SII.Qty - ISNULL(SII.RetQty, 0))), 0) - V.CostPrice AS DECIMAL(18, 2)) AS ProfitPerUnit,
                                 CAST(SUM((CASE WHEN SII.Qty > 0 THEN SII.LineTotal * (SII.Qty - ISNULL(SII.RetQty, 0)) / SII.Qty ELSE 0 END)) - SUM((SII.Qty - ISNULL(SII.RetQty, 0)) * V.CostPrice) AS DECIMAL(18, 2)) AS TotalProfit
                                 FROM Sale.SaleInvoices SI
                                 INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                 INNER JOIN Inventory.InventoryItemVariations V ON V.ID = SII.VariationID
                                 INNER JOIN Inventory.InventoryItems II ON II.ID = V.ItemID
                                 LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                 LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                 WHERE SI.IsDeleted = 0 AND (SII.Qty - ISNULL(SII.RetQty, 0)) > 0 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND II.BrandID =  @BrandID ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND V.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IA.ID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAF.ID = @VarientID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (V.VariationName LIKE @SearchString) ";
                }
                query += @" GROUP BY V.ID, V.VariationName, V.CostPrice, IA.Title, IAF.Title ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ProductName" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedProfitMarginReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedBatchWiseStockReportAsync(GetPagedBatchWiseStockReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT V.ID, V.VariationName AS ProductName, IA.Title AS Category, IAF.Title AS Variant, V.Batch,
                                V.ExpiryDate, V.OpeningStock, V.CostPrice, V.SellingPrice
                                FROM Inventory.InventoryItems I
                                INNER JOIN Inventory.InventoryItemVariations V ON V.ItemID = I.ID
                                LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                WHERE  I.IsDeleted = 0 ";

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND V.CategoryID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND V.FormationID = @VarientID ";
                }
                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND V.ItemID = @ItemID ";
                }
                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND (I.SupplierID = @SupplierID OR @SupplierID IN (SELECT value FROM STRING_SPLIT(I.SupplierID, ','))) ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,I.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (I.Name LIKE @SearchString OR V.VariationName LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(input.GroupByColumn))
                {
                    sortAndOrder.Add($"MyList.{input.GroupByColumn}", "DESC");
                }
                else
                {
                    sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "Batch" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);
                }
                var temp = await DBHelper.GetPagedListAsync<GetPagedBatchWiseStockReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedTaxReportAsync(GetPagedTaxReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT S.ID, S.InvoiceNo, S.InvoiceDate,
                    ISNULL(T.Percentage, 0) AS Tax,
                    SUM((SI.ItemTax * SI.Qty)) + ISNULL(SFD.SubtotalAfterTax - SFD.SubtotalAfterDiscount, 0.00) AS TaxAmount,
                    SFD.SubtotalAfterTax AS NetAmount,
                    ISNULL((SELECT C.Name FROM Patient.Patients C WHERE C.ID = S.CustomerID), 'Walk-in Customer') AS CustomerName
                                    FROM Sale.SaleInvoices S
                                    INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                    INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = S.ID
                                    LEFT JOIN Tax.Taxes T ON T.ID = SFD.TaxID AND SFD.TaxID != 0
                                    WHERE   S.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,S.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND S.SalePersonUsername =  @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    dbValues.Add("@CustomerType", input.CustomerType);
                    query += " AND S.CustomerID =  @CustomerID AND S.CustomerType = @CustomerType ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");

                    query += " AND (";
                    query += " S.InvoiceNo LIKE @SearchString OR ";
                    query += " (SELECT C.Name FROM Patient.Patients C WHERE C.ID = S.CustomerID) LIKE @SearchString)";
                }

                query += " GROUP BY S.ID, S.InvoiceNo, S.InvoiceDate, T.Percentage, SFD.SubtotalAfterTax, SFD.SubtotalAfterDiscount, S.CustomerID, S.CustomerType";

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                  var temp = await DBHelper.GetPagedListAsync<GetPagedTaxReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedDeletedInvoicesReportAsync(GetPagedDeletedInvoicesReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT 
                        S.ID, 
                        S.InvoiceNo, 
                        S.DeletedOn,  
                        SFD.SubtotalAfterTax AS Amount,
                        ISNULL(C.Name, 'Walk-in Customer') AS Customer,
                        ISNULL(NULLIF(U.FullName, ''), ISNULL(NULLIF(S.DeletedBy, ''), '')) AS DeletedBy
                    FROM Sale.SaleInvoices S
                    LEFT JOIN Sale.SaleInvoiceFinancialDetails SFD 
                        ON SFD.InvoiceID = S.ID 
                    LEFT JOIN Patient.Patients C 
                        ON C.ID = S.CustomerID
                    LEFT JOIN Auth.Users U 
                        ON U.Username = S.DeletedBy
                    WHERE S.IsDeleted = 1 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,S.DeletedOn) BETWEEN @StartDate AND @EndDate ";
                }
                 

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    dbValues.Add("@CustomerType", input.CustomerType);
                    query += " AND S.CustomerID =  @CustomerID AND S.CustomerType = @CustomerType ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%"); 
                    query += " AND (S.InvoiceNo LIKE @SearchString OR C.Name LIKE @SearchString OR ISNULL(U.FullName, '') LIKE @SearchString)"; 
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedDeletedInvoicesReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedSupplierOutstandingReportAsync(GetPagedSupplierOutstandingReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT S.ID, S.Name AS SupplierName, MAX(PO.OrderDate) AS DateTime,
                                       SUM(P.SubtotalAfterTax) AS TotalPurchase,
                                       SUM(ISNULL(P.PaidAmount, 0)) AS PaidAmount,
                                       SUM(ISNULL(P.RemainingAmount, 0)) AS Balance
                                FROM Purchase.PurchaseOrders PO
                                INNER JOIN Purchase.Purchases P ON P.PurchaseOrderID = PO.ID
                                INNER JOIN Supplier.Suppliers S ON S.ID = PO.SupplierID
                                WHERE PO.IsDeleted = 0 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND PO.SupplierID =  @SupplierID ";
                }


                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (S.Name LIKE @SearchString ) ";
                }

                query += " GROUP BY S.ID, S.Name ";
                if (input.MinOutstandingAmount > 0)
                {
                    dbValues.Add("@MinOutstanding", input.MinOutstandingAmount.ToString());
                    query += " HAVING SUM(ISNULL(P.RemainingAmount, 0)) >= @MinOutstanding ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedSupplierOutstandingReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedIncomeReportAsync(GetPagedIncomReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT S.ID, S.InvoiceNo, S.InvoiceDate AS DateTime, SUM(SI.UnitCost * SI.Qty) NetAmount,
                             SUM(
                                CASE
                                    WHEN SI.DiscountType = 'percentage' THEN (SI.UnitCost * SI.Qty) * (SI.Discount / 100.0)
                                    WHEN SI.DiscountType = 'value' THEN (SI.Discount * SI.Qty)
                                    ELSE 0
                                END
                                ) + (SFD.SubTotal - SFD.SubtotalAfterDiscount) AS DiscountAmount,
                             SUM(
                                    CASE
                                    WHEN SI.DiscountType = 'percentage' THEN
                                        (((SI.UnitCost * SI.Qty) - ((SI.UnitCost * SI.Qty) * (SI.Discount / 100.0))) * (SI.ItemTax / 100.0))
                                    WHEN SI.DiscountType = 'value' THEN
                                        (((SI.UnitCost * SI.Qty) - (SI.Discount * SI.Qty)) * (SI.ItemTax / 100.0))
                                    ELSE 0.00
                                    END
                                ) + ISNULL(SFD.SubtotalAfterTax - SFD.SubtotalAfterDiscount, 0.00) AS TaxAmount,
                           SFD.SubtotalAfterTax AS TotalIncome,
                           (SELECT STRING_AGG(PM.PaymentMode, ', ') FROM (SELECT DISTINCT X.PaymentMode FROM Sale.SaleInvoicePayments X
                                    WHERE X.InvoiceID = S.ID AND ISNULL(X.TransactionType, 'Payment') <> 'Return') PM) AS PaymentMode,
                           ISNULL(( SELECT C.Name FROM Patient.Patients C WHERE C.ID = S.CustomerID), 'Walk-in Customer') AS Customer
                        FROM Sale.SaleInvoices S
                        INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                        INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = S.ID
                        WHERE  S.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,S.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND S.SalePersonUsername = @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    dbValues.Add("@CustomerType", input.CustomerType);
                    query += " AND S.CustomerID =  @CustomerID AND S.CustomerType = @CustomerType ";
                }
                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");

                    query += " AND (";
                    query += " S.InvoiceNo LIKE @SearchString OR ";
                    query += " (SELECT P.Name FROM Patient.Patients P WHERE P.ID = S.CustomerID) LIKE @SearchString";
                    query += " )";
                }
                query += " GROUP BY S.ID, S.InvoiceNo, S.InvoiceDate, SFD.SubtotalAfterTax, SFD.SubtotalAfterDiscount, S.CustomerID, S.CustomerType, SFD.SubTotal ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedIncomReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedManufacturerWiseSalesReportAsync(GetPagedManufacturerWiseSalesReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT ISNULL(M.Name, 'No Manufacturer') AS Manufacturer, SI.Name AS ProductName, (SI.Qty - ISNULL(SI.RetQty, 0)) AS Qty, (CASE WHEN SI.Qty > 0 THEN SI.LineTotal * (SI.Qty - ISNULL(SI.RetQty, 0)) / SI.Qty ELSE 0 END) AS NetSale
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                 INNER JOIN Inventory.InventoryItems I ON I.ID = SI.ItemID
                                 LEFT JOIN Manufacturer.Manufacturers M ON M.ID = I.ManufacturerID  
                                 WHERE   S.IsDeleted = 0 ";

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,S.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (SI.Name LIKE @SearchString OR M.Name LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(input.GroupByColumn))
                {
                    sortAndOrder.Add($"MyList.{input.GroupByColumn}", "DESC");
                }
                else
                {
                    sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "Manufacturer" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);
                }
                var temp = await DBHelper.GetPagedListAsync<GetPagedManufacturerWiseSalesReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedProductWiseProfitReportAsync(GetPagedProductWiseProfitReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT V.VariationName AS ProductName,IAF.Title AS Variant, IA.Title AS Category, V.CostPrice AS PurchaseRate,
                                 CAST(SUM((CASE WHEN SI.Qty > 0 THEN SI.LineTotal * (SI.Qty - ISNULL(SI.RetQty, 0)) / SI.Qty ELSE 0 END)) / NULLIF(SUM((SI.Qty - ISNULL(SI.RetQty, 0))), 0) AS DECIMAL(18, 2)) AS SaleRate,
                                 SUM((SI.Qty - ISNULL(SI.RetQty, 0))) AS QtySold,
                                 CAST(SUM((SI.Qty - ISNULL(SI.RetQty, 0)) * V.CostPrice) AS DECIMAL(18, 2)) AS TotalCost,
                                 CAST(SUM((CASE WHEN SI.Qty > 0 THEN SI.LineTotal * (SI.Qty - ISNULL(SI.RetQty, 0)) / SI.Qty ELSE 0 END)) AS DECIMAL(18, 2)) AS TotalSale,
                                 CAST(SUM((CASE WHEN SI.Qty > 0 THEN SI.LineTotal * (SI.Qty - ISNULL(SI.RetQty, 0)) / SI.Qty ELSE 0 END)) - SUM((SI.Qty - ISNULL(SI.RetQty, 0)) * V.CostPrice) AS DECIMAL(18, 2)) AS TotalProfit
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                 INNER JOIN Inventory.InventoryItemVariations V ON V.ID = SI.VariationID
                                 INNER JOIN Inventory.InventoryItems II ON II.ID = V.ItemID
                                 LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                 LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                 WHERE  S.IsDeleted = 0 AND (SI.Qty - ISNULL(SI.RetQty, 0)) > 0 ";

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND II.BrandID =  @BrandID ";
                }
                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND V.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IA.ID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAF.ID = @VarientID ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,S.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (V.VariationName LIKE @SearchString) ";
                }
                query += @" GROUP BY V.ID, V.VariationName, V.CostPrice, IAF.Title, IA.Title ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ProductName" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedProductWiseProfitReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedCashCollectionReportAsync(GetPagedCashCollectionReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT P.ID, P.Date AS DateTime, S.InvoiceNo,
                                        CASE WHEN P.TransactionType = 'Return' THEN -P.PaidAmount ELSE P.PaidAmount END AS Amount,
                                        CASE WHEN P.TransactionType = 'Return' THEN ISNULL(P.PaymentMode, '') + ' (Refund)' ELSE P.PaymentMode END AS PaymentMode
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoicePayments P ON P.InvoiceID = S.ID
                                 WHERE   S.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND S.SalePersonUsername = @SalePersonUsername";
                }
                if (!string.IsNullOrEmpty(input.PaymentMode))
                {
                    dbValues.Add("@PaymentMode", input.PaymentMode);
                    query += " AND P.PaymentMode = @PaymentMode";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE, ISNULL(P.Date, S.InvoiceDate)) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (S.InvoiceNo LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedCashCollectionReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedSlowMovingItemsReportAsync(GetPagedProductWiseProfitReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                bool hasDates = !string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate);
                dbValues.Add("@StartDate", hasDates ? Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd") : null);
                dbValues.Add("@EndDate", hasDates ? Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd") : null);

                string query = @"SELECT V.VariationName AS ProductName, IAF.Title AS Variant, IA.Title AS Category,
                                 ISNULL(SUM(SL.NetQty), 0) AS QtySold, MAX(SL.InvoiceDate) AS DateTime, V.OpeningStock
                                 FROM Inventory.InventoryItemVariations V
                                 INNER JOIN Inventory.InventoryItems II ON II.ID = V.ItemID AND II.IsDeleted = 0
                                 LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                 LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                 LEFT JOIN (
                                     SELECT SI.VariationID, S.InvoiceDate, (SI.Qty - ISNULL(SI.RetQty, 0)) AS NetQty
                                     FROM Sale.SaleInvoiceItems SI
                                     INNER JOIN Sale.SaleInvoices S ON S.ID = SI.InvoiceID AND S.IsDeleted = 0
                                     WHERE (@StartDate IS NULL OR CONVERT(DATE, S.InvoiceDate) BETWEEN @StartDate AND @EndDate)
                                 ) SL ON SL.VariationID = V.ID
                                 WHERE 1 = 1 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND II.SupplierID =  @SupplierID ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND II.ManufacturerID =  @ManufacturerID ";
                }

                if (input.BrandID > 0)
                {
                    dbValues.Add("@BrandID", input.BrandID.ToString());
                    query += " AND II.BrandID =  @BrandID ";
                }

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND II.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND V.ID =  @VariationID ";
                }

                if (input.CategoryID > 0)
                {
                    dbValues.Add("@CategoryID", input.CategoryID.ToString());
                    query += " AND IA.ID = @CategoryID ";
                }
                if (input.VarientID > 0)
                {
                    dbValues.Add("@VarientID", input.VarientID.ToString());
                    query += " AND IAF.ID = @VarientID ";
                }


                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (V.VariationName LIKE @SearchString) ";
                }
                query += @" GROUP BY V.ID, V.VariationName, IAF.Title, IA.Title, V.OpeningStock ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "QtySold" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "ASC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedSlowItemsReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedCustomerOutstandingReportAsync(GetPagedCustomerOutstandingReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT
                                  ISNULL((SELECT C.Name
                                      FROM Patient.Patients C
                                      WHERE C.ID = S.CustomerID), 'Walk-in Customer') AS Customer,
                                 MAX(S.InvoiceDate) AS DateTime, SUM(SFD.SubtotalAfterTax) AS TotalSale, SUM(SFD.PaidAmount) AS PaidAmount , SUM(SFD.RemainingAmount) AS Balance
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = S.ID
                                 WHERE   S.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    dbValues.Add("@CustomerType", input.CustomerType);
                    query += " AND S.CustomerID =  @CustomerID AND S.CustomerType = @CustomerType ";
                }


                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,S.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");

                    query += " AND (S.InvoiceNo LIKE @SearchString OR (SELECT C.Name FROM Patient.Patients C WHERE C.ID = S.CustomerID) LIKE @SearchString) ";
                }
                query += @" GROUP BY S.CustomerType, S.CustomerID ";
                if (input.MinOutstandingAmount > 0)
                {
                    dbValues.Add("@MinOutstanding", input.MinOutstandingAmount.ToString());
                    query += " HAVING SUM(SFD.RemainingAmount) >= @MinOutstanding ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "Customer" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "ASC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedCustomerOutstandingReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetPagedManualStockAdjustmentReportAsync(GetPagedManualAdjustmentReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT H.ID,H.TimeStamp AS DateTime, V.VariationName AS ProductName,
                               H.Status AS AdjustmentType, H.Qty
                               FROM Inventory.InventoryHistories H
                               INNER JOIN Inventory.InventoryItemVariations V ON V.ID = H.VariationID
                               INNER JOIN Inventory.InventoryItems I ON I.ID = V.ItemID
                               WHERE  I.IsDeleted = 0
                               AND H.Status IN ('Stock-In', 'Stock-Out') AND H.Soruce = 'System' ";

                if (input.ItemID > 0)
                {
                    dbValues.Add("@ItemID", input.ItemID.ToString());
                    query += " AND I.ID =  @ItemID ";
                }

                if (input.VariationID > 0)
                {
                    dbValues.Add("@VariationID", input.VariationID.ToString());
                    query += " AND V.ID =  @VariationID ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND H.AddedBy =  @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,H.TimeStamp) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (V.VariationName LIKE @SearchString) ";
                }
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ProductName" : input.SortBy)}",
                                     string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedManualAdjustmentReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetUserActivityReportAsync(GetPagedUserActivityReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT A.ID, CONVERT(VARCHAR(19), A.CreatedOn, 120) AS DateTime,
                                        ISNULL(NULLIF(U.FullName, ''), A.Username) AS [User],
                                        A.Action AS ActionType, A.EntityID AS ReferenceNo,
                                        ISNULL(A.EntityType, '') + ' - ' + ISNULL(A.Details, '') AS Remarks
                                 FROM dbo.ActivityLogs A
                                 LEFT JOIN Auth.Users U ON U.Username = A.Username
                                 WHERE 1 = 1 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,A.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND A.Username = @SalePersonUsername";
                }
                if (!string.IsNullOrEmpty(input.ActionType))
                {
                    // Filter values are INSERT / UPDATE / DELETE / LOGIN ...; log actions look like SALE_CREATE.
                    string pattern;
                    switch (input.ActionType.ToUpperInvariant())
                    {
                        case "INSERT": pattern = "%CREATE%"; break;
                        case "UPDATE": pattern = "%UPDATE%"; break;
                        case "DELETE": pattern = "%DELETE%"; break;
                        case "RETURN": pattern = "%RETURN%"; break;
                        case "LOGIN": pattern = "LOGIN%"; break;
                        default: pattern = "%" + input.ActionType + "%"; break;
                    }
                    dbValues.Add("@ActionType", pattern);
                    query += " AND A.Action LIKE @ActionType";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (U.FullName LIKE @SearchString OR A.Username LIKE @SearchString OR A.Action LIKE @SearchString OR A.Details LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                 string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedUserActivityReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<string, string>> GetDailyActivityReportAsync(GetPagedDailyActivityReportInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT ISNULL(NULLIF(U.FullName, ''), A.Username) AS [User],
                                    COUNT(CASE WHEN A.Action = 'SALE_CREATE' THEN 1 END) AS TotalSales,
                                    COUNT(CASE WHEN A.Action LIKE 'SALE_RETURN%' THEN 1 END) AS TotalReturns,
                                    COUNT(CASE WHEN A.Action IN ('PURCHASE_RECEIVE', 'PURCHASE_ORDER_SAVE') THEN 1 END) AS TotalPurchases,
                                    COUNT(*) AS UserActivityCount
                                FROM dbo.ActivityLogs A
                                LEFT JOIN Auth.Users U ON U.Username = A.Username
                                WHERE A.Action NOT IN ('LOGIN', 'LOGOUT', 'LOGIN_FAILED') ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,A.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND A.Username = @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (U.FullName LIKE @SearchString OR A.Username LIKE @SearchString) ";
                }

                query += @" GROUP BY ISNULL(NULLIF(U.FullName, ''), A.Username) ";
                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "[User]" : input.SortBy)}",
                                 string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedDailyActivityReportOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }
    }
}