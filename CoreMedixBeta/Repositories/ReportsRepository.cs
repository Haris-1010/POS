using CoreMedixBeta.Enums;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Reports.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
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
                            M.Name AS ManufacturerName,
                            FORMAT(SI.InvoiceDate, 'dd MMM yyyy') AS DateTime,
                            (SELECT C.Name
                                FROM Patient.Patients C
                                WHERE C.ID = SI.CustomerID
                            ) AS CustomerName
                        FROM Sale.SaleInvoiceItems I
                        INNER JOIN Sale.SaleInvoices SI ON I.InvoiceID = SI.ID
                        INNER JOIN Inventory.InventoryItems INI ON INI.ID = I.ItemID
                        INNER JOIN Manufacturer.Manufacturers M ON M.ID = INI.ManufacturerID
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
        COUNT(SSI.ID) AS ItemsCount,
        SUM(SSI.Qty) AS TotalQty,
        C.Name AS CustomerName,
        FD.SubTotal AS GrossAmount,
        FD.DiscountAmount AS Discount,
        FD.SubtotalAfterTax AS NetAmount
    FROM Sale.SaleInvoices AS SI
    LEFT JOIN Sale.SaleInvoiceItems AS SSI ON SSI.InvoiceID = SI.ID
    LEFT JOIN Sale.SaleInvoiceFinancialDetails AS FD ON FD.InvoiceID = SI.ID
    LEFT JOIN Sale.SaleInvoicePayments AS SIP ON SIP.InvoiceID = SI.ID
    LEFT JOIN Inventory.InventoryItems INI ON INI.ID = SSI.ItemID
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

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND SI.SalePersonUsername = @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(input.PaymentMode))
                {
                    dbValues.Add("@PaymentMode", input.PaymentMode);
                    query += " AND SIP.PaymentMode = @PaymentMode ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND SI.InvoiceNo LIKE @SearchString ";
                }
                #endregion

                // Grouping
                query += @"   GROUP BY SI.InvoiceNo, SI.CreatedOn, C.Name, FD.SubTotal, FD.DiscountAmount, FD.SubtotalAfterTax ";

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

                string query = @"SELECT SI.ID, IIV.VariationName AS Name, IA.Title AS Category, SII.Qty, IIV.CostPrice AS PurchaseRate, SII.UnitCost AS SaleRate, SII.UnitCost * SII.Qty AS GrossAmount, SII.DiscountType,
                                    CASE
									WHEN SII.DiscountType = 'percentage' THEN
										 (SII.Discount * SII.UnitCost) / 100
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
									WHEN SII.DiscountType = 'percentage' THEN
										((SII.UnitCost - IIV.CostPrice) * SII.Qty)
										- ((SII.UnitCost * SII.Qty) * (SII.Discount / 100.0))
									ELSE
										((SII.UnitCost - IIV.CostPrice) * SII.Qty)
										- SII.Discount * SII.Qty
								    END AS Profit
                                    FROM Sale.SaleInvoices AS SI
                                    INNER JOIN Sale.SaleInvoiceItems AS SII ON SII.InvoiceID = SI.ID
                                    INNER JOIN Inventory.InventoryItemVariations AS IIV ON IIV.ID = SII.VariationID
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
                    query += " AND (IIV.VariationName LIKE @SearchString) ";
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

                string query = @"SELECT	DISTINCT SI.ID,SI.InvoiceNo, SI.CreatedOn AS [DateTime], I.Name AS Product, IH.Qty AS QtyReturned,
					                             SFD.ReturnAmount,
				                                (SELECT C.Name
						                                  FROM Patient.Patients C
						                                  WHERE C.ID = SI.CustomerID) AS Customer,
				                                ISNULL(NULLIF(U.FullName, ''),
				                                       ISNULL(NULLIF(IH.AddedBy, ''),
				                                              ISNULL(NULLIF(SI.LastModifiedBy, ''), ''))) AS [User]
                                 FROM Sale.SaleInvoices AS SI
                                 INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = SI.ID
                                 LEFT JOIN Inventory.InventoryHistories AS IH ON IH.SaleInvoiceID = SI.ID
                                 INNER JOIN Inventory.InventoryItems I ON I.ID =  IH.ItemID
                                 LEFT JOIN Auth.Users U ON U.Username = ISNULL(NULLIF(IH.AddedBy, ''), SI.LastModifiedBy)
                                   WHERE   IH.Soruce = 'Sale Return' AND IH.SaleInvoiceID IS NOT NULL AND SI.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.CreatedOn) BETWEEN @StartDate AND @EndDate ";
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
                                ( SELECT C.Name
                                          FROM Patient.Patients C
                                          WHERE C.ID = e.CustomerID) AS CustomerName,
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
                                    'Payment - ' + SIP.PaymentMode AS Description,
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
                                    'Return - ' + SIP.PaymentMode AS Description,
                                    SIP.PaidAmount AS Debit,
                                    0.00 AS Credit, 
                                    3 AS SortOrder
                                FROM Sale.SaleInvoicePayments SIP
                                INNER JOIN Sale.SaleInvoices SI ON SI.ID = SIP.InvoiceID
                                WHERE SI.IsDeleted = 0
                                AND SIP.TransactionType = 'Return'
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

                string query = @"SELECT II.Name AS [ProductName], IAA.Title AS Varient, IA.Title AS [Category], M.Name as Manufacturer,
                                SUM(SII.Qty) AS [QtySold], SUM(SII.LineTotal) AS [TotalSale], COUNT(DISTINCT SI.ID) AS [SaleCount]
                                FROM Sale.SaleInvoiceItems SII
                                INNER JOIN Sale.SaleInvoices SI ON SI.ID = SII.InvoiceID AND SI.IsDeleted = 0
                                INNER JOIN Inventory.InventoryItems II ON II.ID = SII.ItemID AND II.IsDeleted = 0
                                INNER JOIN Inventory.InventoryItemVariations IV ON IV.ID = SII.VariationID
                                LEFT JOIN Inventory.ItemAttributes IA ON IV.CategoryID = IA.ID
                                LEFT JOIN Inventory.ItemAttributes IAA ON IV.FormationID = IAA.ID
                                INNER JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
                                WHERE SII.Qty > 0 AND SI.IsDeleted = 0 ";

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

                string query = @"SELECT IV.ID, IV.VariationName AS ProductName, IAV.Title AS Varient, IAC.Title AS Category, M.Name AS Manufacturer,
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
                                WHERE   IV.OpeningStock <= IV.ReorderPoint AND II.IsDeleted = 0 ";

                if (input.MinimumQty > 0)
                {
                    dbValues.Add("@MinimumQty", input.MinimumQty.ToString());
                    query += " AND IV.OpeningStock = @MinimumQty ";
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

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,SI.CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }

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
                                    M.Name AS Manufacturer, 
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
                                INNER JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
                                WHERE II.IsDeleted = 0  ";

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

                string query = @"SELECT II.ID, IIV.VariationName, IAV.Title AS Variant, IAC.Title AS Category, M.Name AS Manufacturer,
                                 IIV.OpeningStock, IIV.CostPrice, (IIV.OpeningStock * IIV.CostPrice) AS TotalValue
                                 FROM Inventory.InventoryItems AS II
                                 INNER JOIN Inventory.InventoryItemVariations AS IIV ON II.ID = IIV.ItemID
                                 LEFT JOIN Inventory.ItemAttributes AS IAC ON IIV.CategoryID = IAC.ID AND IAC.AttributeType = 'Category'  
                                 LEFT JOIN Inventory.ItemAttributes AS IAV ON IIV.FormationID = IAV.ID AND IAV.AttributeType = 'Formation'  
                                 INNER JOIN Manufacturer.Manufacturers AS M ON II.ManufacturerID = M.ID
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
							            H.Qty,  (H.Qty * V.SellingPrice) AS Balance,
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

                string query = @"SELECT *
                                FROM (
	                                   SELECT PO.ID, PO.ReferenceNo, PO.OrderDate AS DateTime, PO.Status, SUM(P.SubtotalAfterTax) AS Debit, SUM(P.PaidAmount) AS Credit,
                                       SUM(P.RemainingAmount) AS RunningBalance, S.Name AS SupplierName, P.AdditionalNotes AS [Description],
                                        PO.SupplierID 
                                FROM Purchase.PurchaseOrders PO
                                INNER JOIN Purchase.Purchases P ON P.PurchaseOrderID = PO.ID
                                INNER JOIN Supplier.Suppliers S ON S.ID = PO.SupplierID
                                WHERE PO.IsDeleted = 0
                                GROUP BY PO.ID, PO.ReferenceNo, PO.OrderDate, PO.Status, S.Name, p.AdditionalNotes,  PO.SupplierID 

                                UNION ALL

                                SELECT PO.ID, PO.ReferenceNo, PO.OrderDate, PO.Status, SUM(POI.LineTotal) AS Debit, SUM(0.00) AS Credit,
                                        SUM(POI.LineTotal) AS RunningBalance, S.Name AS SupplierName, '' AS [Description],
                                        PO.SupplierID 
                                FROM Purchase.PurchaseOrders PO
                                INNER JOIN Purchase.PurchaseOrderItems POI ON POI.PurchaseOrderID = PO.ID
                                INNER JOIN Supplier.Suppliers S ON S.ID = PO.SupplierID
                                WHERE PO.IsPurchaseReceived = 0
                                AND PO.IsDeleted = 0
                                GROUP BY PO.ID, PO.ReferenceNo, PO.OrderDate, PO.Status, S.Name,  PO.SupplierID 
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

                string query = @"SELECT V.VariationName AS ProductName, IAF.Title AS Variant, IA.Title AS Category, V.CostPrice AS PurchaseRate,  V.SellingPrice AS SaleRate,
                                 SUM(SII.Qty) AS QtySold, (V.SellingPrice - V.CostPrice) AS ProfitPerUnit, SUM((SII.Qty * (V.SellingPrice - V.CostPrice) )) AS TotalProfit
                                 FROM Sale.SaleInvoices SI
                                 INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                 INNER JOIN Inventory.InventoryItemVariations V ON V.ID = SII.VariationID
                                 INNER JOIN Inventory.InventoryItems II ON II.ID = V.ItemID
                                 LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                 LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                 WHERE SI.IsDeleted = 0 ";

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
                query += @" GROUP BY V.VariationName, V.CostPrice, V.SellingPrice, IA.Title, IAF.Title ";
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
                    (SUM((SI.ItemTax * SI.Qty)) + ISNULL(SFD.SubtotalAfterTax - SFD.SubtotalAfterDiscount, 0.00)) / 100 AS Tax,
                     SUM((SI.ItemTax * SI.Qty)) + ISNULL(SFD.SubtotalAfterTax - SFD.SubtotalAfterDiscount, 0.00) 
                        AS TaxAmount, SUM(SI.LineTotal) NetAmount ,
                                     (SELECT C.Name
                                                  FROM Patient.Patients C
                                                  WHERE C.ID = S.CustomerID)  AS CustomerName
                                    FROM Sale.SaleInvoices S
                                    INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                    INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = S.ID
                                    LEFT JOIN Tax.Taxes T ON T.ID = SFD.TaxID AND SFD.TaxID != 0
                                    INNER JOIN Inventory.InventoryItems I ON I.ID = SI.ItemID
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
                    query += " (CASE WHEN S.CustomerType = 'Patient' THEN ";
                    query += "     (SELECT U.Name FROM InstaUser.[User] U WHERE U.Username = S.CustomerID) ";
                    query += "  ELSE ";
                    query += "     (SELECT C.Name FROM Patient.Patients C WHERE C.ID = S.CustomerID) ";
                    query += " END) LIKE @SearchString)";
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
                        C.Name AS Customer,
                        ISNULL(NULLIF(U.FullName, ''), ISNULL(NULLIF(S.DeletedBy, ''), '')) AS DeletedBy
                    FROM Sale.SaleInvoices S
                    INNER JOIN Sale.SaleInvoiceFinancialDetails SFD 
                        ON SFD.InvoiceID = S.ID 
                    INNER JOIN Patient.Patients C 
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

                string query = @"SELECT PO.ID, S.Name AS SupplierName, PO.OrderDate AS DateTime, ISNULL(P.SubtotalAfterTax, PO.SubTotal) AS TotalPurchase,
	                                    ISNULL(P.PaidAmount, 0) AS PaidAmount, ISNULL(P.RemainingAmount, PO.SubTotal) AS Balance
                                FROM Purchase.PurchaseOrders PO
                                LEFT JOIN Purchase.Purchases P ON P.PurchaseOrderID = PO.ID
                                INNER JOIN Supplier.Suppliers S ON S.ID = PO.SupplierID  
                                WHERE  PO.IsDeleted = 0 ";

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND PO.SupplierID =  @SupplierID ";
                }

                if (input.MinOutstandingAmount > 0)
                {
                    dbValues.Add("@MinOutstanding", input.MinOutstandingAmount.ToString());
                    query += " AND P.RemainingAmount <  @MinOutstanding ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (S.Name LIKE @SearchString ) ";
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
				                        WHEN SI.DiscountType = 'value' THEN (SI.Discount)
				                        ELSE 0
			                        END
		                            ) + (SFD.SubTotal - SFD.SubtotalAfterDiscount) AS DiscountAmount,
		                         SUM(
                                    CASE
                                    WHEN SI.DiscountType = 'percentage' THEN
                                        (((SI.UnitCost * SI.Qty) - ((SI.UnitCost * SI.Qty) * (SI.Discount / 100.0))) * (SI.ItemTax / 100.0))
                                    WHEN SI.DiscountType = 'value' THEN
                                        (((SI.UnitCost * SI.Qty) - SI.Discount) * (SI.ItemTax / 100.0))
                                    ELSE 0.00
                                    END
			                        ) + ISNULL(SFD.SubtotalAfterTax - SFD.SubtotalAfterDiscount, 0.00) AS TaxAmount,

	                           SFD.SubtotalAfterTax AS TotalIncome, P.PaymentMode,
                                ( SELECT C.Name
                                      FROM Patient.Patients C
                                      WHERE C.ID = S.CustomerID)  AS Customer
                        FROM Sale.SaleInvoices S
                        INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                        INNER JOIN Sale.SaleInvoiceFinancialDetails SFD ON SFD.InvoiceID = S.ID
                        LEFT JOIN Sale.SaleInvoicePayments P ON P.InvoiceID = S.ID
                        LEFT JOIN Tax.Taxes T ON T.ID = SFD.TaxID AND SFD.TaxID != 0
                        INNER JOIN Inventory.InventoryItems I ON I.ID = SI.ItemID
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
                query += " GROUP BY S.ID, S.InvoiceNo, S.InvoiceDate, T.Percentage, SFD.SubtotalAfterTax, SFD.SubtotalAfterDiscount, S.CustomerID, S.CustomerType, P.PaymentMode, SFD.SubTotal ";
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

                string query = @"SELECT M.Name AS Manufacturer, SI.Name AS ProductName, SI.Qty, SI.LineTotal AS NetSale
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                 INNER JOIN Inventory.InventoryItems I ON I.ID = SI.ItemID
                                 INNER JOIN Manufacturer.Manufacturers M ON M.ID = I.ManufacturerID  
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

                string query = @"SELECT V.VariationName AS ProductName,IAF.Title AS Variant, IA.Title AS Category, V.CostPrice AS PurchaseRate, V.SellingPrice AS SaleRate,
                                 SUM(SI.Qty) AS QtySold, SUM(SI.Qty * V.CostPrice) AS TotalCost, SUM(SI.LineTotal) AS TotalSale, SUM((SI.Qty * (V.SellingPrice - V.CostPrice) )) AS TotalProfit
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                 INNER JOIN Inventory.InventoryItemVariations V ON V.ID = SI.VariationID
                                 INNER JOIN Inventory.InventoryItems II ON II.ID = V.ItemID
                                 LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                 LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                 WHERE  S.IsDeleted = 0 ";

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
                query += @" GROUP BY V.VariationName, V.CostPrice, V.SellingPrice, IAF.Title, IA.Title ";
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

                string query = @"SELECT S.ID, S.InvoiceDate AS DateTime, S.InvoiceNo, P.PaidAmount AS Amount, P.PaymentMode 
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

                    query += " AND CONVERT(DATE,S.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
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

                string query = @"SELECT V.VariationName AS ProductName,IAF.Title AS Variant, IA.Title AS Category, V.CostPrice AS PurchaseRate, V.SellingPrice AS SaleRate,
                                 SUM(SI.Qty) AS QtySold, SUM(SI.Qty * V.CostPrice) AS TotalCost, SUM(SI.LineTotal) AS TotalSale,
                                 SUM((SI.Qty * (V.SellingPrice - V.CostPrice) )) AS TotalProfit, V.OpeningStock, MAX(S.InvoiceDate) AS DateTime
                                 FROM Sale.SaleInvoices S
                                 INNER JOIN Sale.SaleInvoiceItems SI ON SI.InvoiceID = S.ID
                                 INNER JOIN Inventory.InventoryItemVariations V ON V.ID = SI.VariationID
                                 INNER JOIN Inventory.InventoryItems II ON II.ID = V.ItemID
                                 LEFT JOIN Inventory.ItemAttributes IA ON IA.ID = V.CategoryID AND IA.AttributeType = 'Category'
                                 LEFT JOIN Inventory.ItemAttributes IAF ON IAF.ID = V.FormationID AND IAF.AttributeType = 'Formation'
                                 WHERE S.IsDeleted = 0 ";

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

                    query += " AND CONVERT(DATE,S.InvoiceDate) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (V.VariationName LIKE @SearchString) ";
                }
                query += @" GROUP BY V.VariationName, V.CostPrice, V.SellingPrice, IAF.Title, IA.Title, V.OpeningStock ";
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
                                  (SELECT C.Name
                                      FROM Patient.Patients C
                                      WHERE C.ID = S.CustomerID) AS Customer,
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

                if (input.MinOutstandingAmount > 0)
                {
                    dbValues.Add("@MinOutstanding", input.MinOutstandingAmount.ToString());
                    query += " AND SFD.RemainingAmount <  @MinOutstanding ";
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

                    query += " AND ";
                    query += " S.InvoiceNo LIKE @SearchString OR ";  
                    query += "     (SELECT C.Name FROM Patient.Patients C WHERE C.ID = S.CustomerID) ";
                    query += "   LIKE @SearchString";
                }
                query += @" GROUP BY S.CustomerType, S.CustomerID ";

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
                               AND H.Status IN ('Stock-In', 'Stock-Out') ";

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

                string query = @"SELECT A.ID, A.TimeStamp DateTime, U.Name [User], A.EventType ActionType, A.AffectedID ReferenceNo,
                                 PARSENAME(A.[Table], 1) + ' - ' + Description Remarks
                                 FROM Pharmacy.AuditTrails A
                                 INNER JOIN InstaUser.[User] U ON U.Username = A.UserID
                                 WHERE A.MasterUsername = @MasterUsername ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,A.TimeStamp) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND A.UserID = @SalePersonUsername";
                }
                if (!string.IsNullOrEmpty(input.ActionType))
                {
                    dbValues.Add("@ActionType", input.ActionType);
                    query += " AND A.EventType = @ActionType";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND U.Name LIKE @SearchString  ";
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

                string query = @"SELECT U.Name [User],
                                    COUNT(CASE WHEN A.[Table] = 'Sale.SaleInvoices' AND A.EventType = 'INSERT' THEN 1 END) AS TotalSales,
                                    COUNT(CASE WHEN A.[Table] = 'Sale.SaleInvoices' AND A.EventType = 'RETURN' THEN 1 END) AS TotalReturns,
                                    COUNT(CASE WHEN A.[Table] = 'Purchase.Purchases' AND A.EventType = 'INSERT' THEN 1 END) AS TotalPurchases,
                                    COUNT(*) AS UserActivityCount
                                FROM Pharmacy.AuditTrails A
                                INNER JOIN InstaUser.[User] U ON U.Username = A.UserID
                                WHERE  A.[Table] IN (
                                    'Sale.SaleInvoices',
                                    'Purchase.Purchases'
                                ) ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,A.TimeStamp) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(input.SalePersonUsername))
                {
                    dbValues.Add("@SalePersonUsername", input.SalePersonUsername);
                    query += " AND A.UserID = @SalePersonUsername ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND U.Name LIKE @SearchString  ";
                }

                query += @"GROUP BY U.Name ";
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