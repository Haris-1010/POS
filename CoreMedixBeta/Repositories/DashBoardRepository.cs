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
        public async Task<GetDashBoardCardsOutputDto> GetDashBoardCardsAsync(RequestByMasterUsernameDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"WITH FilteredInvoices AS (
                                SELECT SI.ID, SI.CustomerID
                                FROM Sale.SaleInvoices AS SI
                                WHERE SI.CreatedOn >= CAST(GETDATE() AS DATE)
                                AND SI.CreatedOn < DATEADD(DAY, 1, CAST(GETDATE() AS DATE))
                                AND SI.IsDeleted = 0
                                )
                            SELECT
                                -- Total Profit Today
                                COALESCE(SUM((IIV.SellingPrice - IIV.CostPrice) * SII.Qty), 0) AS TotalProfitToday,

                                -- FIXED: Total Sale Today using a separate subquery to prevent duplicate sum
                                (SELECT COALESCE(SUM(FD.SubtotalAfterTax), 0)
                                 FROM Sale.SaleInvoices SI
                                 INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                 WHERE SI.CreatedOn >= CAST(GETDATE() AS DATE)
                                 AND SI.CreatedOn < DATEADD(DAY, 1, CAST(GETDATE() AS DATE))
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

                                                              -- Today’s Stock Sold Value
                                (SELECT COALESCE(SUM(SII.Qty * IIV.CostPrice), 0)
                                 FROM Sale.SaleInvoices SI
                                 INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID
                                 WHERE SI.CreatedOn >= CAST(GETDATE() AS DATE)
                                   AND SI.CreatedOn < DATEADD(DAY, 1, CAST(GETDATE() AS DATE))
                                   AND SI.IsDeleted = 0) AS TodayStockSoldValue,


                                -- Total Expired Items
                                (SELECT COUNT(CASE WHEN IIV.ExpiryDate <= DATEADD(DAY, 7, GETDATE()) THEN 1 END)
                                 FROM Inventory.InventoryItems II
                                 INNER JOIN Inventory.InventoryItemVariations IIV ON II.ID = IIV.ItemID
                                 --WHERE IIV.ExpiryDate <= GETDATE()
                                 WHERE II.IsDeleted = 0) AS TotalExpiryItems
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

        public async Task<DashboardChartsDto> GetDashboardChartsAsync()
        {
            var charts = new DashboardChartsDto();

            string dailyQuery = @"WITH Days AS (
                                    SELECT CAST(DATEADD(DAY, -6, CAST(GETDATE() AS DATE)) AS DATE) AS D
                                    UNION ALL
                                    SELECT DATEADD(DAY, 1, D) FROM Days WHERE D < CAST(GETDATE() AS DATE)
                                  ),
                                  Sales AS (
                                    SELECT CAST(SI.CreatedOn AS DATE) AS D, COALESCE(SUM(FD.SubtotalAfterTax), 0) AS SaleAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceFinancialDetails FD ON FD.InvoiceID = SI.ID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                                    GROUP BY CAST(SI.CreatedOn AS DATE)
                                  ),
                                  Profit AS (
                                    SELECT CAST(SI.CreatedOn AS DATE) AS D,
                                           COALESCE(SUM((IIV.SellingPrice - IIV.CostPrice) * SII.Qty), 0) AS ProfitAmount
                                    FROM Sale.SaleInvoices SI
                                    INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                    INNER JOIN Inventory.InventoryItemVariations IIV ON IIV.ID = SII.VariationID
                                    WHERE SI.IsDeleted = 0 AND SI.CreatedOn >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                                    GROUP BY CAST(SI.CreatedOn AS DATE)
                                  )
                                  SELECT CONVERT(varchar(10), Days.D, 120) AS [Date],
                                         ISNULL(Sales.SaleAmount, 0) AS SaleAmount,
                                         ISNULL(Profit.ProfitAmount, 0) AS ProfitAmount
                                  FROM Days
                                  LEFT JOIN Sales ON Sales.D = Days.D
                                  LEFT JOIN Profit ON Profit.D = Days.D
                                  ORDER BY Days.D
                                  OPTION (MAXRECURSION 7)";

            string topProductsQuery = @"SELECT TOP 5 SII.Name AS Label, COALESCE(SUM(SII.LineTotal), 0) AS Value
                                        FROM Sale.SaleInvoices SI
                                        INNER JOIN Sale.SaleInvoiceItems SII ON SII.InvoiceID = SI.ID
                                        WHERE SI.IsDeleted = 0
                                          AND SI.CreatedOn >= DATEADD(DAY, -29, CAST(GETDATE() AS DATE))
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

            try { charts.DailySales = await DBHelper.GetListAsync<DailySalePoint>(dailyQuery) ?? new List<DailySalePoint>(); }
            catch (Exception) { }

            try { charts.TopProducts = await DBHelper.GetListAsync<LabelValueItem>(topProductsQuery) ?? new List<LabelValueItem>(); }
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