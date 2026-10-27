using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Purchases.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class PurchasesRepository
    {
        public async Task<GetDetailedPurchasesOutputDto> GetDetailedPurchases(GetDetailedPurchasesInputDto input)
        {
            try
            {
                GetDetailedPurchasesOutputDto result = new GetDetailedPurchasesOutputDto();
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.ID.ToString() },
                    { "@PurchaseOrderID", input.PurchaseOrderID.ToString() }
                };

                string query = @"SELECT
                           P.ID,   PO.ReferenceNo,  PO.Status,   S.Name AS SupplierName,
                           S.City AS SupplierCity,
                           FORMAT(P.PurchaseReceivedAt, 'dd MMM yyyy') AS PurchaseReceivedAtFormated,
                           P.PurchaseReceivedAt, S.Phone AS SupplierPhone, P.PurchaseOrderID, P.SubtotalAfterTax, T.Percentage AS Tax,
                           P.TaxID, T.Title AS TaxTitle, P.SubTotal, P.SubTotalAfterDiscount, P.PaidAmount, P.RemainingAmount, P.ShippingDetails,
                           P.AdditionalNotes,   P.DiscountType,  P.DiscountAmount
                    FROM Purchase.Purchases P
                    INNER JOIN Purchase.PurchaseOrders PO ON PO.ID = P.PurchaseOrderID
                    LEFT JOIN Tax.Taxes T ON T.ID = P.TaxID
                    INNER JOIN Supplier.Suppliers S ON S.ID = PO.SupplierID
                    WHERE P.ID = @ID;  ";

                result.Purchase = await DBHelper.GetAsync<PurchseDetailsDto>(query, Params: dbValues);

                if (result.Purchase != null && result.Purchase.ID > 0)
                {
                    query = @"SELECT I.PurchaseID, I.Name, I.Qty,I.BonusQty, I.ItemTax, I.UnitCost,V.Weight,
                    I.UnitCostWithTax, I.LineTotal, WU.Title AS WeightUnit, F.Title AS Formation,I.ItemTax, V.ID AS VariationID, V.ItemID
                    FROM Purchase.PurchaseItems I
                    INNER JOIN Inventory.InventoryItemVariations V ON V.ID = I.VariationID
                    LEFT JOIN Inventory.ItemAttributes F ON F.ID = V.FormationID AND F.AttributeType = 'Formation'
                    LEFT JOIN Inventory.ItemAttributes WU ON WU.ID = V.WeightUnitID AND WU.AttributeType = 'WeightUnit'
                    WHERE I.PurchaseID = @ID ";

                    result.PurchaseItems = await DBHelper.GetListAsync<PurchaseItemsDto>(query, Params: dbValues);

                    query = @"SELECT FORMAT(Date, 'dd MMM yyyy') DateFormated,Date, PaidAmount, PaymentMode,AttachmentURL
                            FROM Purchase.PurchasePayments WHERE PurchaseID = @ID ORDER BY Date ASC";
                    result.PurcahsePayments = await DBHelper.GetListAsync<PurchasePaymentsDto>(query, Params: dbValues);

                    return result;
                }
                else
                {
                    return new GetDetailedPurchasesOutputDto();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetDetailedPurchasesOutputDto();
            }
        }

        public async Task<Tuple<string, string>> GetPagedPurchases(GetPagedPurchasesInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT P.ID, P.PurchaseOrderID, PO.ReferenceNo, FORMAT(P.PurchaseReceivedAt, 'dd MMM yyyy') PurchaseDate,
                       S.Name AS SuplierName, P.PaidAmount, P.SubTotalAfterTax, P.RemainingAmount
                       FROM Purchase.Purchases P
                       INNER JOIN Purchase.PurchaseOrders PO ON PO.ID = P.PurchaseOrderID
                       INNER JOIN Supplier.Suppliers S ON PO.SupplierID = S.ID
                       WHERE PO.IsDeleted = 0 AND PO.IsPurchaseReceived = 1 ";

                string countQuery = @"SELECT COUNT(*) FROM Purchase.Purchases P
                       INNER JOIN Purchase.PurchaseOrders PO ON PO.ID = P.PurchaseOrderID
                       INNER JOIN Purchase.PurchasePayments PP ON P.ID = PP.PurchaseID
                       WHERE  PO.IsDeleted = 0 AND PO.IsPurchaseReceived = 1 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND (P.PurchaseReceivedAt BETWEEN @StartDate AND @EndDate)  ";
                    countQuery += " AND (P.PurchaseReceivedAt BETWEEN @StartDate AND @EndDate) ";
                }
                if (!string.IsNullOrEmpty(input.ReferenceNo))
                {
                    dbValues.Add("@ReferenceNo", input.ReferenceNo.ToString());
                    query += " AND PO.ReferenceNo = @ReferenceNo ";
                    countQuery += " AND PO.ReferenceNo = @ReferenceNo ";
                }
                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND PO.SupplierID = @SupplierID ";
                    countQuery += " AND PO.SupplierID = @SupplierID ";
                }
                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (PO.ReferenceNo LIKE @SearchString) ";
                    countQuery += " AND (PO.ReferenceNo LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    {
                        $"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                        string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder
                    }
                };

                var data = await DBHelper.GetPagedListAsync<GetPagedPurchasesOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<int> SavePurchase(SavePurchaseInputDto input)
        {
            try
            {
                if (input.Purchase.ID == 0)
                {
                    input.Purchase.CreatedOn = ServerConfig.DateTimeNow;
                    input.Purchase.CreatedBy = string.IsNullOrWhiteSpace(input.CurrentUsername) ? "System" : input.CurrentUsername;
                }
                else
                {
                    input.Purchase.LastModifiedOn = ServerConfig.DateTimeNow;
                    input.Purchase.LastModifiedBy = string.IsNullOrWhiteSpace(input.CurrentUsername) ? "System" : input.CurrentUsername;
                }
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@InputJSON", JsonConvert.SerializeObject(input) }
                };

                string query = @"EXEC Purchase.SavePurchase @InputJSON";
                var response = await StoredProcedureHelper.GetSPAsync<StoredProcedureResponseVM>(query, Params: dbValues);
                return response.ID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
    }
}