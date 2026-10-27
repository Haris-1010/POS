using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.PurchaseOrders;
using CoreMedixBeta.Models.PurchaseOrders.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class PurchaseOrdersRepository
    {
        public async Task<bool> DeletePurchaseOrder(DeleteRequestDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.ID.ToString()},
                    { "@DeletedBy", input.DeletedBy },
                    { "@DeletedOn", ServerConfig.DateTimeNow.ToString("yyyy-MM-dd HH:mm:ss") },
                };
                string query = @"UPDATE Purchase.PurchaseOrders SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn  WHERE ID = @ID ";
                return await DBHelper.ExecuteQueryAsync(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<GetDetailedPurchaseOrderOutputDto> GetDetailedPurchaseOrder(GetDetailedPurchaseOrderInputDto input)
        {
            try
            {
                GetDetailedPurchaseOrderOutputDto result = new GetDetailedPurchaseOrderOutputDto();
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.ID.ToString() },
                };
                string query = string.Empty;
                if (input.IsPrint == true)
                {
                    query = @"SELECT PO.ID,PO.ReferenceNo,FORMAT(PO.OrderDate, 'dd MMM yyyy') OrderDateFormatted, S.Name AS SupplierName,
                       PO.Status, PO.SubTotal, PO.SupplierID, PO.OrderDate, PO.BusinessLocation,  PO.IsPurchaseReceived, S.Phone SupplierPhone
                       FROM Purchase.PurchaseOrders PO
                       LEFT JOIN Supplier.Suppliers S ON PO.SupplierID = S.ID
                       INNER JOIN Purchase.PurchaseOrderItems OI ON OI.PurchaseOrderID = PO.ID
                       WHERE PO.ID = @ID";
                }
                else
                {
                    query = @"SELECT PO.ID,PO.ReferenceNo,FORMAT(PO.OrderDate, 'dd MMM yyyy') OrderDateFormatted, S.Name AS SupplierName,
                       PO.Status, PO.SubTotal, PO.SupplierID, PO.OrderDate, PO.BusinessLocation, PO.IsPurchaseReceived, S.Phone SupplierPhone
                       FROM Purchase.PurchaseOrders PO
                       LEFT JOIN Supplier.Suppliers S ON PO.SupplierID = S.ID 
                       INNER JOIN Purchase.PurchaseOrderItems OI ON OI.PurchaseOrderID = PO.ID
                       WHERE  PO.ID = @ID";
                }
                result.Order = await DBHelper.GetAsync<PurchaseOrderDto>(query, Params: dbValues);

                if (result.Order != null && result.Order.ID > 0)
                {
                    query = @"SELECT * FROM Purchase.PurchaseOrderItems WHERE PurchaseOrderID = @ID";
                    result.OrderItems = await DBHelper.GetListAsync<PurchaseOrderItem>(query, Params: dbValues);
                    return result;
                }
                else
                {
                    return new GetDetailedPurchaseOrderOutputDto();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetDetailedPurchaseOrderOutputDto();
            }
        }

        public async Task<Tuple<string, string>> GetPagedPurchaseOrders(GetPagedPurchaseOrdersInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT PO.ID,PO.ReferenceNo,FORMAT(PO.OrderDate, 'dd MMM yyyy') Date, S.Name AS SuplierName ,S.Phone AS SuplierPhone,PO.Status,
                                 PO.SubTotal, PO.IsPurchaseReceived, P.PaidAmount, P.RemainingAmount, COUNT(OI.ID) AS TotalItems, P.ID AS PurchaseID
                                 FROM Purchase.PurchaseOrders PO
                                 LEFT JOIN Supplier.Suppliers S ON PO.SupplierID = S.ID
                                 INNER JOIN Purchase.PurchaseOrderItems OI ON OI.PurchaseOrderID = PO.ID
                                 LEFT JOIN Purchase.Purchases P ON P.PurchaseOrderID = PO.ID
                                 WHERE PO.IsDeleted = 0 ";

                string countQuery = @"SELECT COUNT(*) FROM Purchase.PurchaseOrders PO
                                    LEFT JOIN Supplier.Suppliers S ON PO.SupplierID = S.ID
                                    INNER JOIN Purchase.PurchaseOrderItems OI ON OI.PurchaseOrderID = PO.ID
                                    WHERE PO.IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.FilterType))
                {
                    switch (input.FilterType)
                    {
                        case "PendingOrders":
                            query += " AND PO.Status = 'Pending' ";
                            countQuery += " AND PO.Status = 'Pending'  ";
                            break;

                        case "Ordered":
                            query += " AND PO.Status = 'Ordered' ";
                            countQuery += " AND PO.Status = 'Ordered' ";
                            break;

                        case "ReceivedOrders":
                            query += " AND PO.Status = 'Received' ";
                            countQuery += " AND PO.Status = 'Received' ";
                            break;

                        case "All":
                        default:
                            // No additional filter
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND (PO.OrderDate BETWEEN @StartDate AND @EndDate)  ";
                    countQuery += " AND (PO.OrderDate BETWEEN @StartDate AND @EndDate) ";
                }

                if (!string.IsNullOrEmpty(input.ReferenceNo))
                {
                    dbValues.Add("@ReferenceNo", input.ReferenceNo.ToString());
                    query += " AND PO.ReferenceNo=@ReferenceNo ";
                    countQuery += " AND PO.ReferenceNo=@ReferenceNo ";
                }
                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND PO.SupplierID = @SupplierID ";
                    countQuery += " AND PO.SupplierID = @SupplierID ";
                }
                if (!string.IsNullOrEmpty(input.Status))
                {
                    dbValues.Add("@Status", input.Status);
                    query += " AND PO.Status = @Status ";
                    countQuery += " AND PO.Status = @Status ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (PO.ReferenceNo LIKE @SearchString) ";
                    countQuery += " AND (PO.ReferenceNo LIKE @SearchString) ";
                }

                query += @"GROUP BY PO.ID, PO.ReferenceNo, PO.OrderDate, S.Name,S.Phone, PO.Status, PO.SubTotal, PO.IsPurchaseReceived, P.PaidAmount, P.RemainingAmount,P.ID ";
                countQuery += @"GROUP BY PO.ID, PO.ReferenceNo, PO.OrderDate, S.Name,S.Phone, PO.Status, PO.SubTotal, PO.IsPurchaseReceived, P.PaidAmount, P.RemainingAmount,P.ID ";

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>();
                sortAndOrder.Add($"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                                 string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder);

                var temp = await DBHelper.GetPagedListAsync<GetPagedPurchaseOrdersOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                var data = temp.Results;
                string jr = JsonConvert.SerializeObject(data);
                string total = JsonConvert.SerializeObject(temp.pager.TotalRecords);

                return Tuple.Create(jr, total);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<string> GenerateReferenceNoAsync(RequestByMasterUsernameDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT TOP 1 ReferenceNo FROM Purchase.PurchaseOrders
                                ORDER BY ID DESC";

                var referenceNo = await DBHelper.GetSingleColumnAsync(query, dbValues);

                if (!string.IsNullOrEmpty(referenceNo))
                {
                    var numberPart = referenceNo.Replace("PO", "").TrimStart('-');
                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        return $"PO-{currentNumber + 1}";
                    }
                }

                return "PO-1";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public async Task<int> SavePurchaseOrder(SavePurchaseOrderInputDto input)
        {
            try
            {
                if (input.Order.ID == 0) { 
                    input.Order.CreatedOn = ServerConfig.DateTimeNow;
                    input.Order.CreatedBy = string.IsNullOrWhiteSpace(input.CurrentUsername) ? "System" : input.CurrentUsername;
                }
                else { 
                    input.Order.LastModifiedOn = ServerConfig.DateTimeNow;
                    input.Order.LastModifiedBy = string.IsNullOrWhiteSpace(input.CurrentUsername) ? "System" : input.CurrentUsername;
                }
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@InputJSON", JsonConvert.SerializeObject(input) }
                };

                string query = @"EXEC Purchase.SavePurchaseOrder @InputJSON";
                var response = await StoredProcedureHelper.GetSPAsync<StoredProcedureResponseVM>(query, Params: dbValues);
                return response.ID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<int> ClearDueAmountAsync(ClearDueAmountInputDto input)
        {
            try
            {
                if (input.PurchasePayment.PurchaseID > 0)
                {
                    DateTime LastModifiedOn = ServerConfig.DateTimeNow;

                    Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                    {"@DueAmount", input.RemainingAmount.ToString()},
                    {"@LastModifiedOn", LastModifiedOn.ToString("yyyy-MM-dd HH:mm:ss") },
                    {"@LastModifiedBy", input.LastModifiedBy},
                };

                    string query = @"UPDATE Purchase.Purchases SET PaidAmount = PaidAmount + @DueAmount, RemainingAmount = RemainingAmount - @DueAmount, LastModifiedOn = @LastModifiedOn, LastModifiedBy = @LastModifiedBy
                               WHERE PurchaseOrderID = @ID";
                    bool isUpdated = await DBHelper.ExecuteQueryAsync(query, dbValues);
                    if (isUpdated)
                    {
                        await DBHelper.InsertAsync(input.PurchasePayment);
                        return input.ID;
                    }
                    else return 0;
                }
                else
                    return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<GetPurchaseOrdersInfoOutputDto> GetGetPurchaseOrdersInfoAsync()
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @" SELECT COUNT(PO.ReferenceNo) AllOrders,
	                                     COUNT(CASE WHEN PO.Status = 'Ordered' THEN 1 END) TotalOrders,
	                                     COUNT(CASE WHEN PO.Status = 'Received' THEN 1 END) TotalReceived,
	                                     COUNT(CASE WHEN PO.Status = 'Pending' THEN 1 END) TotalPending
                                  FROM Purchase.PurchaseOrders PO
                                  WHERE  PO.IsDeleted = 0 ";

                return await DBHelper.GetAsync<GetPurchaseOrdersInfoOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetPurchaseOrdersInfoOutputDto();
            }
        }
    }
}