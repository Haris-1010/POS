using CoreMedixBeta.Models.DashBoard;
using CoreMedixBeta.Models.DashBoard.DTOs;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventoryItems.DTOs;
using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class DashBoardRepository
    {
        /// <summary>Earliest recorded sale date, used as the real start of the "All Time" filter
        /// instead of an arbitrary fixed date (which would otherwise pad the trend chart with
        /// years of empty data before the business existed).</summary>
        public async Task<DateTime?> GetEarliestSaleDateAsync()
        {
            try
            {
                string result = await DBHelper.GetSingleColumnAsync(
                    "SELECT MIN(CAST(CreatedOn AS DATE)) FROM Sale.SaleInvoices WHERE IsDeleted = 0");

                return DateTime.TryParse(result, out DateTime parsed) ? parsed : (DateTime?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<GetDashBoardCardsOutputDto> GetDashBoardCardsAsync(RequestByMasterUsernameDTO input, DateTime fromDate, DateTime toDate)
        {
            try
            {
                DateTime toExclusive = toDate.Date.AddDays(1);

                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@From", fromDate.Date.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@ToExclusive", toExclusive.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                string query = @"WITH FilteredInvoices AS (
                                SELECT SI.ID, SI.CustomerID
                                FROM Sale.SaleInvoices AS SI
                                WHERE SI.CreatedOn >= @From
                                AND SI.CreatedOn < @ToExclusive
                                AND SI.IsDeleted = 0
                                )
                            SELECT
                                -- Total Profit (selected range)
                                COALESCE(SUM((IIV.SellingPrice - IIV.CostPrice) * SII.Qty), 0) AS TotalProfitToday,

                                -- FIXED: Total Sale using a separate subquery to prevent duplicate sum
                                (SELECT COALESCE(SUM(FD.SubtotalAfterTax), 0)
                                 FROM Sale.SaleInvoices SI
                                 INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                 WHERE SI.CreatedOn >= @From
                                 AND SI.CreatedOn < @ToExclusive
                                 AND SI.IsDeleted = 0) AS TotalSaleToday,

                                -- FIXED: Unique count of invoices today
                                COUNT(DISTINCT FI.ID) AS TotalInvoicesToday,

                                -- Total Customers Today
                                COUNT(DISTINCT FI.CustomerID) AS TotalCustomers,

                                -- Total Stock Value
                                (SELECT COALESCE(SUM(IIV.OpeningStock * IIV.CostPrice), 0)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 WHERE II.IsDeleted = 0) AS TotalStockValue,

                               -- Total Stock
                                (SELECT SUM(IIV.OpeningStock)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 WHERE II.IsDeleted = 0) AS TotalItems,

                               -- Total Items
                                (SELECT COUNT(IIV.ID)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 WHERE II.IsDeleted = 0) AS TotalItems,

                                -- Total Low Stock Items
                                (SELECT COUNT(IIV.ID)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 WHERE II.IsDeleted = 0 AND IIV.OpeningStock <= IIV.ReorderPoint) AS LowStockItems,

                                -- Total New Arrivals Items
                                (SELECT COUNT(CASE WHEN II.CreatedOn >= DATEADD(DAY, -15, GETDATE()) THEN IIV.ID END)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 WHERE   II.IsDeleted = 0) AS NewArrivalItems,

                                                              -- Stock Sold Value (selected range)
                                (SELECT COALESCE(SUM(SII.Qty * IIV.CostPrice), 0)
                                 FROM Sale.SaleInvoices SI
                                 INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID
                                 WHERE SI.CreatedOn >= @From
                                   AND SI.CreatedOn < @ToExclusive
                                   AND SI.IsDeleted = 0) AS TodayStockSoldValue,


                                -- Total Expired Items
                                (SELECT COUNT(CASE WHEN IIV.ExpiryDate <= DATEADD(DAY, 7, GETDATE()) THEN 1 END)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 --WHERE IIV.ExpiryDate <= GETDATE()
                                 WHERE II.IsDeleted = 0) AS TotalExpiryItems,

                                -- Amount Receivable: current outstanding balance across all invoices (live snapshot, not date-filtered)
                                (SELECT COALESCE(SUM(CASE WHEN (FD.SubtotalAfterTax - ISNULL(Paid.TotalPaid, 0)) > 0
                                                          THEN (FD.SubtotalAfterTax - ISNULL(Paid.TotalPaid, 0)) ELSE 0 END), 0)
                                 FROM Sale.SaleInvoices SI2
                                 INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI2.ID
                                 LEFT JOIN (
                                     SELECT InvoiceID, SUM(PaidAmount) AS TotalPaid
                                     FROM Sale.SaleInvoicePayments
                                     GROUP BY InvoiceID
                                 ) Paid ON Paid.InvoiceID = SI2.ID
                                 WHERE SI2.IsDeleted = 0) AS TotalReceivable
                            FROM FilteredInvoices FI
                            LEFT JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = FI.ID
                            LEFT JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID ";

                return await DBHelper.GetAsync<GetDashBoardCardsOutputDto>(query, Params: dbValues);
            }
            catch (Exception)
            {
                return new GetDashBoardCardsOutputDto();
            }
        }

        /// <summary>
        /// "Sales &amp; Profit" trend and "Top Products" are scoped to [fromDate, toDate] (inclusive).
        /// "Monthly Sales" (always last 6 months) and "Payment Modes" (not currently shown in the UI)
        /// stay independent of the selected filter - they're a fixed "big picture" overview.
        /// </summary>
        public async Task<DashboardChartsDto> GetDashboardChartsAsync(DateTime fromDate, DateTime toDate)
        {
            var charts = new DashboardChartsDto();

            DateTime from = fromDate.Date;
            DateTime toExclusive = toDate.Date.AddDays(1);
            int spanDays = (toDate.Date - from).Days + 1;

            string granularity = spanDays <= 31 ? "day" : (spanDays <= 731 ? "month" : "year");
            charts.TrendGranularity = granularity;

            Dictionary<string, string> rangeParams = new Dictionary<string, string>
            {
                { "@From", from.ToString("yyyy-MM-dd HH:mm:ss") },
                { "@ToExclusive", toExclusive.ToString("yyyy-MM-dd HH:mm:ss") }
            };

            string dailyQuery;
            if (granularity == "day")
            {
                dailyQuery = @"WITH Days AS (
                                    SELECT CAST(@From AS DATE) AS D
                                    UNION ALL
                                    SELECT DATEADD(DAY, 1, D) FROM Days WHERE D < CAST(DATEADD(DAY, -1, @ToExclusive) AS DATE)
                                  ),
                                  Sales AS (
                                    SELECT CAST(SI.CreatedOn AS DATE) AS D, COALESCE(SUM(FD.SubtotalAfterTax), 0) AS SaleAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                    GROUP BY CAST(SI.CreatedOn AS DATE)
                                  ),
                                  Profit AS (
                                    SELECT CAST(SI.CreatedOn AS DATE) AS D,
                                           COALESCE(SUM((IIV.SellingPrice - IIV.CostPrice) * SII.Qty), 0) AS ProfitAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                    INNER JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                    GROUP BY CAST(SI.CreatedOn AS DATE)
                                  )
                                  SELECT CONVERT(varchar(10), Days.D, 120) AS [Date],
                                         ISNULL(Sales.SaleAmount, 0) AS SaleAmount,
                                         ISNULL(Profit.ProfitAmount, 0) AS ProfitAmount
                                  FROM Days
                                  LEFT JOIN Sales ON Sales.D = Days.D
                                  LEFT JOIN Profit ON Profit.D = Days.D
                                  ORDER BY Days.D
                                  OPTION (MAXRECURSION 31)";
            }
            else if (granularity == "month")
            {
                dailyQuery = @"WITH M AS (
                                    SELECT DATEFROMPARTS(YEAR(@From), MONTH(@From), 1) AS M
                                    UNION ALL
                                    SELECT DATEADD(MONTH, 1, M) FROM M
                                    WHERE M < DATEFROMPARTS(YEAR(DATEADD(DAY, -1, @ToExclusive)), MONTH(DATEADD(DAY, -1, @ToExclusive)), 1)
                                  ),
                                  Sales AS (
                                    SELECT DATEFROMPARTS(YEAR(SI.CreatedOn), MONTH(SI.CreatedOn), 1) AS M,
                                           COALESCE(SUM(FD.SubtotalAfterTax), 0) AS SaleAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                    GROUP BY DATEFROMPARTS(YEAR(SI.CreatedOn), MONTH(SI.CreatedOn), 1)
                                  ),
                                  Profit AS (
                                    SELECT DATEFROMPARTS(YEAR(SI.CreatedOn), MONTH(SI.CreatedOn), 1) AS M,
                                           COALESCE(SUM((IIV.SellingPrice - IIV.CostPrice) * SII.Qty), 0) AS ProfitAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                    INNER JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                    GROUP BY DATEFROMPARTS(YEAR(SI.CreatedOn), MONTH(SI.CreatedOn), 1)
                                  )
                                  SELECT CONVERT(varchar(7), M.M, 120) AS [Date],
                                         ISNULL(Sales.SaleAmount, 0) AS SaleAmount,
                                         ISNULL(Profit.ProfitAmount, 0) AS ProfitAmount
                                  FROM M
                                  LEFT JOIN Sales ON Sales.M = M.M
                                  LEFT JOIN Profit ON Profit.M = M.M
                                  ORDER BY M.M
                                  OPTION (MAXRECURSION 400)";
            }
            else
            {
                dailyQuery = @"WITH Y AS (
                                    SELECT YEAR(@From) AS Y
                                    UNION ALL
                                    SELECT Y + 1 FROM Y WHERE Y < YEAR(DATEADD(DAY, -1, @ToExclusive))
                                  ),
                                  Sales AS (
                                    SELECT YEAR(SI.CreatedOn) AS Y, COALESCE(SUM(FD.SubtotalAfterTax), 0) AS SaleAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                    GROUP BY YEAR(SI.CreatedOn)
                                  ),
                                  Profit AS (
                                    SELECT YEAR(SI.CreatedOn) AS Y,
                                           COALESCE(SUM((IIV.SellingPrice - IIV.CostPrice) * SII.Qty), 0) AS ProfitAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                    INNER JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                    GROUP BY YEAR(SI.CreatedOn)
                                  )
                                  SELECT CAST(Y.Y AS varchar(4)) AS [Date],
                                         ISNULL(Sales.SaleAmount, 0) AS SaleAmount,
                                         ISNULL(Profit.ProfitAmount, 0) AS ProfitAmount
                                  FROM Y
                                  LEFT JOIN Sales ON Sales.Y = Y.Y
                                  LEFT JOIN Profit ON Profit.Y = Y.Y
                                  ORDER BY Y.Y
                                  OPTION (MAXRECURSION 100)";
            }

            string topProductsQuery = @"SELECT TOP 5 SII.Name AS Label, COALESCE(SUM(SII.LineTotal), 0) AS Value
                                        FROM Sale.SaleInvoices SI
                                        INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                        WHERE SI.IsDeleted = 0
                                          AND SI.CreatedOn >= @From AND SI.CreatedOn < @ToExclusive
                                          AND ISNULL(SII.TransactionStatus, '') <> 'RETURNED'
                                        GROUP BY SII.Name
                                        ORDER BY Value DESC";

            string monthlyQuery = @"WITH M AS (
                                      SELECT CAST(DATEADD(MONTH, -5, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS DATE) AS M
                                      UNION ALL
                                      SELECT DATEADD(MONTH, 1, M) FROM M
                                      WHERE M < DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                                    )
                                    SELECT CONVERT(varchar(7), M.M, 120) AS Label,
                                           ISNULL((
                                             SELECT SUM(FD.SubtotalAfterTax)
                                             FROM Sale.SaleInvoices SI
                                             INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                             WHERE SI.IsDeleted = 0
                                               AND SI.CreatedOn >= M.M
                                               AND SI.CreatedOn < DATEADD(MONTH, 1, M.M)
                                           ), 0) AS Value
                                    FROM M
                                    ORDER BY M.M
                                    OPTION (MAXRECURSION 6)";

            string paymentModeQuery = @"SELECT ISNULL(NULLIF(P.PaymentMode, ''), 'Other') AS Label,
                                               COALESCE(SUM(P.PaidAmount), 0) AS Value
                                        FROM Sale.SaleInvoicePayments P
                                        INNER JOIN Sale.SaleInvoices SI ON SI.ID = P.InvoiceID
                                        WHERE SI.IsDeleted = 0
                                          AND SI.CreatedOn >= DATEADD(DAY, -29, CAST(GETDATE() AS DATE))
                                        GROUP BY P.PaymentMode
                                        ORDER BY Value DESC";

            try { charts.DailySales = await DBHelper.GetListAsync<DailySalePoint>(dailyQuery, Params: rangeParams) ?? new List<DailySalePoint>(); }
            catch (Exception) { }

            try { charts.TopProducts = await DBHelper.GetListAsync<LabelValueItem>(topProductsQuery, Params: rangeParams) ?? new List<LabelValueItem>(); }
            catch (Exception) { }

            try { charts.MonthlySales = await DBHelper.GetListAsync<LabelValueItem>(monthlyQuery) ?? new List<LabelValueItem>(); }
            catch (Exception) { }

            try { charts.PaymentModes = await DBHelper.GetListAsync<LabelValueItem>(paymentModeQuery) ?? new List<LabelValueItem>(); }
            catch (Exception) { }

            return charts;
        }

        public async Task<List<GetDashboardSettingOutputDto>> GetUserDashboardSettingsAsync(RequestByMasterUsernameDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT ID, Title, [Order], IsVisible, CardID, UserID 
                       FROM Dashboard.DashboardSettings  ";

                return await DBHelper.GetListAsync<GetDashboardSettingOutputDto>(query, Params: dbValues);
            }
            catch (Exception)
            {
                return new List<GetDashboardSettingOutputDto>();
            }
        }

        public async Task<bool> SavetDashBoardSettingAsync(SaveDashboardSettingInputDto input)
        {
            try
            {
                bool isSaved = false;

                var toUpdate = input.DashboardSettings.Where(x => x.ID > 0).ToList();

                if (toUpdate.Any())
                {
                    string updateQuery = @"UPDATE Dashboard.DashboardSettings
                                   SET [Order] = @Order,
                                       IsVisible = @IsVisible
                                   WHERE CardID = @CardID 
                                     ";
                    try
                    {
                        foreach (var item in toUpdate)
                        {
                            var dbValues = new Dictionary<string, string>
                            {
                                { "@Order", item.Order.ToString() },
                                { "@IsVisible", item.IsVisible.ToString() },
                                { "@CardID", item.CardID.ToString() },
                                { "@UserID", item.UserID },
                            };
                            isSaved = await DBHelper.ExecuteQueryAsync(updateQuery, dbValues);
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else
                {
                    if (input.DashboardSettings.Count > 0)
                    {
                        isSaved = await DBHelper.InsertListAsync(input.DashboardSettings);
                    }
                }

                return isSaved;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}