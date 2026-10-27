using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Sales;
using CoreMedixBeta.Models.Sales.DTOs;
using CoreMedixBeta.Models.Taxes.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class SalesRepository
    {
        public async Task<GetSaleInvoiceOutputDto> GetSaleInvoice(GetSaleInvoiceInputDto input)
        {
            try
            {
                GetSaleInvoiceOutputDto result = new GetSaleInvoiceOutputDto();
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.InvoiceID.ToString() },
                };

                string query = @"SELECT TOP 1 
                    S.*,
                    (SELECT P.Name 
                     FROM Patient.Patients P 
                     WHERE P.ID = S.CustomerID) AS CustomerName
                FROM Sale.SaleInvoices S
                WHERE S.ID = @ID 
                  AND S.IsDeleted = 0
                ";
                result.SaleInvoice = await DBHelper.GetAsync<SaleInvoiceDto>(query, Params: dbValues);

                if (result.SaleInvoice != null && result.SaleInvoice.ID > 0)
                {
                    query = @"SELECT * FROM Sale.SaleInvoiceItems WHERE InvoiceID = @ID";
                    result.SaleInvoiceItems = await DBHelper.GetListAsync<SaleInvoiceItem>(query, Params: dbValues);

                    query = @"SELECT * FROM Sale.SaleInvoicePayments WHERE InvoiceID = @ID ORDER BY Date ASC";
                    result.SaleInvoicePayments = await DBHelper.GetListAsync<SaleInvoicePayment>(query, Params: dbValues);

                    query = @"SELECT TOP 1 * FROM Sale.SaleInvoiceFinancialDetails WHERE InvoiceID = @ID";
                    result.SaleInvoiceFinancialDetails = await DBHelper.GetAsync<SaleInvoiceFinancialDetail>(query, Params: dbValues);
                    return result;
                }
                else
                {
                    return new GetSaleInvoiceOutputDto();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetSaleInvoiceOutputDto();
            }
        }

        public async Task<bool> DeleteSaleAsync(DeleteSaleInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@InvoiceID", input.ID.ToString()},
                    { "@DeletedBy", input.DeletedBy },
                    { "@DeletedOn", ServerConfig.DateTimeNow.ToString("yyyy-MM-dd HH:mm:ss") },
                };
                string query = @"UPDATE Sale.SaleInvoices SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn
                                WHERE ID = @InvoiceID";

                string deleteQuery = @"DELETE FROM Sale.SaleInvoiceItems WHERE InvoiceID = @InvoiceID";

                bool isDeleted = await DBHelper.ExecuteQueryAsync(query, Params: dbValues);

                if (isDeleted) await DBHelper.ExecuteQueryAsync(deleteQuery, dbValues);

                return isDeleted;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<Tuple<string, string>> GetPagedSalesAsync(GetPagedSalesInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"
                SELECT DISTINCT SI.ID, SI.InvoiceNo, FORMAT(SI.InvoiceDate, 'dd MMM yyyy') AS InvoiceDate,
                       F.PaidAmount, F.RemainingAmount, F.SubtotalAfterTax AS NetTotal,
                       C.Name AS CustomerName,
                       C.Phone AS Phone
                FROM Sale.SaleInvoices AS SI
                INNER JOIN Sale.SaleInvoiceFinancialDetails F ON F.InvoiceID = SI.ID
                LEFT JOIN Sale.SaleInvoicePayments SIP ON SIP.InvoiceID = SI.ID
                LEFT JOIN Patient.Patients C ON C.ID = SI.CustomerID
                WHERE SI.IsDeleted = 0";


                string countQuery = @"
                SELECT COUNT(*) 
                FROM Sale.SaleInvoices AS SI
                INNER JOIN Sale.SaleInvoiceFinancialDetails F ON F.InvoiceID = SI.ID
                INNER JOIN Sale.SaleInvoicePayments SIP ON SIP.InvoiceID = SI.ID
                LEFT JOIN Patient.Patients C ON C.ID = SI.CustomerID
                WHERE SI.IsDeleted = 0";


                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));
                    query += " AND (SI.InvoiceDate BETWEEN @StartDate AND @EndDate) ";
                    countQuery += " AND (SI.InvoiceDate BETWEEN @StartDate AND @EndDate) ";
                }

                if (!string.IsNullOrEmpty(input.PaymentMode))
                {
                    dbValues.Add("@PaymentMode", input.PaymentMode);
                    query += " AND SIP.PaymentMode = @PaymentMode";
                    countQuery += " AND SIP.PaymentMode = @PaymentMode";
                }

                if (!string.IsNullOrEmpty(input.CustomerID))
                {
                    dbValues.Add("@CustomerID", input.CustomerID);
                    query += " AND SI.CustomerID = @CustomerID";
                    countQuery += " AND SI.CustomerID = @CustomerID";
                }

                if (input.TotalAmount > 0)
                {
                    dbValues.Add("@TotalAmount", input.TotalAmount.ToString());
                    query += " AND  F.SubtotalAfterTax = @TotalAmount";
                    countQuery += " AND F.SubtotalAfterTax = @TotalAmount";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (SI.InvoiceNo LIKE @SearchString OR C.Name LIKE @SearchString) ";
                    countQuery += " AND (SI.InvoiceNo LIKE @SearchString OR C.Name LIKE @SearchString) "; 
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    {
                        $"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                        string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder
                    }
                };

                var data = await DBHelper.GetPagedListAsync<GetPagedSalesOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);
                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<int> SaveSaleAsync(SaveSaleInputDto input)
        {
            try
            {
                bool IsSaved = false;
                int SaleInvoiceId = 0;
                if (input.SaleInvoicePayments == null)
                {
                    input.SaleInvoicePayments = new List<SaleInvoicePayment>();
                }

                bool hasReturnItems = input.SaleInvoiceItems != null &&
                                      input.SaleInvoiceItems.Any(i => i.TransactionStatus?.ToUpper() == "RETURNED");
                long historyCursor = 0;
                if (hasReturnItems && !string.IsNullOrWhiteSpace(input.CurrentUsername))
                {
                    historyCursor = await DBHelper.GetScalerAsync("SELECT ISNULL(MAX(ID), 0) FROM Inventory.InventoryHistories");
                }

                if (input.SaleInvoice.ID == 0)
                {
                    string newInvoiceNo = await GetInvoicePrefixAsync();
                    input.SaleInvoice.InvoiceNo = newInvoiceNo;

                    input.SaleInvoice.CreatedOn = ServerConfig.DateTimeNow;
                    SaleInvoiceId = int.Parse(await DBHelper.InsertAndGetIdAsync(input.SaleInvoice));
                    if (SaleInvoiceId > 0)
                    {
                        try
                        {
                            if (input.SaleInvoiceItems.Count > 0)
                            {
                                try
                                {
                                    input.SaleInvoiceItems.ForEach(item => item.InvoiceID = SaleInvoiceId);
                                    await DBHelper.InsertListAsync(input.SaleInvoiceItems);
                                    await StampReturnUserAsync(SaleInvoiceId, historyCursor, input.CurrentUsername);
                                }
                                catch (Exception)
                                {
                                    await RollbackSaleInvoice(SaleInvoiceId);
                                }
                            }

                            if (input.SaleInvoiceFinancialDetails != null)
                            {
                                try
                                {
                                    input.SaleInvoiceFinancialDetails.InvoiceID = SaleInvoiceId;
                                    await DBHelper.InsertAsync(input.SaleInvoiceFinancialDetails);
                                }
                                catch (Exception)
                                {
                                    await RollbackSaleInvoice(SaleInvoiceId);
                                }
                            }

                            if (input.SaleInvoicePayments.Count > 0)
                            {
                                try
                                {
                                    input.SaleInvoicePayments.ForEach(item => item.InvoiceID = SaleInvoiceId);
                                    await DBHelper.InsertListAsync(input.SaleInvoicePayments);
                                }
                                catch (Exception)
                                {
                                    await RollbackSaleInvoice(SaleInvoiceId);
                                }
                            }

                            if (input.SaleInvoice.ReferralOrderID > 0)
                            {
                                try
                                {
                                    await DBHelper.ExecuteQueryAsync(
                                        @"UPDATE Pharmacy.ReferralOrders
                                        SET IsConvertedToInvoice = 1, ConvertedToInvoiceOn = @InvoiceDateTime,
                                        ConvertedToInvoiceBy = @InvoiceCreatedBy, InvoiceID = @InvoiceID
                                        WHERE ID = @ReferralOrderID;",
                                           new Dictionary<string, string>
                                    {
                                        {"@ReferralOrderID", input.SaleInvoice.ReferralOrderID.ToString() },
                                        {"@InvoiceDateTime", input.SaleInvoice.CreatedOn.ToString() },
                                        {"@InvoiceID", SaleInvoiceId.ToString() },
                                        {"@InvoiceCreatedBy", input.SaleInvoice.CreatedBy }
                                    });
                                }
                                catch (Exception)
                                {
                                }
                            }

                            IsSaved = true;
                        }
                        catch (Exception)
                        {
                            await RollbackSaleInvoice(SaleInvoiceId);
                        }
                    }
                }
                else // Invoice Update Case
                {
                    int InvoiceID = input.SaleInvoice.ID;
                    input.SaleInvoice.LastModifiedOn = ServerConfig.DateTimeNow;
                    Dictionary<string, string> dbValues = new Dictionary<string, string>
                    {
                        {"@InvoiceID", InvoiceID.ToString()},
                    };
                    try
                    {
                        IsSaved = await DBHelper.UpdateAsync(input.SaleInvoice, "WHERE ID = @InvoiceID", dbValues);
                    }
                    catch (Exception)
                    {
                        return 0;
                    }

                    if (IsSaved)
                    {
                        if (input.SaleInvoiceItems.Count > 0)
                        {
                            try
                            {
                                string deleteQuery = @"DELETE FROM Sale.SaleInvoiceItems WHERE InvoiceID = @InvoiceID";
                                await DBHelper.ExecuteQueryAsync(deleteQuery, dbValues);

                                input.SaleInvoiceItems.ForEach(item => item.InvoiceID = InvoiceID);
                                await DBHelper.InsertListAsync(input.SaleInvoiceItems);
                                await StampReturnUserAsync(InvoiceID, historyCursor, input.CurrentUsername);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (input.SaleInvoicePayments.Count > 0)
                        {
                            try
                            {
                                string deleteQuery = @"DELETE FROM Sale.SaleInvoicePayments WHERE InvoiceID = @InvoiceID";
                                await DBHelper.ExecuteQueryAsync(deleteQuery, dbValues);

                                input.SaleInvoicePayments.ForEach(item => item.InvoiceID = InvoiceID);
                                await DBHelper.InsertListAsync(input.SaleInvoicePayments);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (input.SaleInvoiceFinancialDetails != null)
                        {
                            try
                            {
                                string deleteQuery = @"DELETE FROM Sale.SaleInvoiceFinancialDetails WHERE InvoiceID = @InvoiceID";
                                await DBHelper.ExecuteQueryAsync(deleteQuery, dbValues);

                                input.SaleInvoiceFinancialDetails.InvoiceID = InvoiceID;
                                await DBHelper.InsertAsync(input.SaleInvoiceFinancialDetails);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        SaleInvoiceId = InvoiceID;
                    }
                }

                return SaleInvoiceId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        private async Task StampReturnUserAsync(int invoiceId, long historyCursor, string username)
        {
            if (string.IsNullOrWhiteSpace(username) || historyCursor <= 0) return;

            try
            {
                string query = @"UPDATE Inventory.InventoryHistories
                                 SET AddedBy = @AddedBy
                                 WHERE Soruce = 'Sale Return'
                                   AND SaleInvoiceID = @InvoiceID
                                   AND ID > @HistoryCursor
                                   AND AddedBy IS NULL";

                await DBHelper.ExecuteQueryAsync(query, new Dictionary<string, string>
                {
                    { "@AddedBy", username },
                    { "@InvoiceID", invoiceId.ToString() },
                    { "@HistoryCursor", historyCursor.ToString() }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<bool> RollbackSaleInvoice(int ID)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@InvoiceID", ID.ToString()},
                };
                string query = @"DELETE FROM Sale.SaleInvoices WHERE ID = @InvoiceID;
                                DELETE FROM Sale.SaleInvoiceItems WHERE InvoiceID = @InvoiceID;
                                DELETE FROM Sale.SaleInvoicePayments WHERE InvoiceID = @InvoiceID;
                                DELETE FROM Sale.SaleInvoiceFinancialDetails WHERE InvoiceID = @InvoiceID;";
                return await DBHelper.ExecuteQueryAsync(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<GetSalesInfoOutputDto> GetSalesInfoAsync(GetSaleInvoiceInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>()
                {
                };

                string query = @"SELECT COUNT(SI.ID) AS TotalInvoices,
                               SUM(F.RemainingAmount) AS TotalPendingAmount,
                               SUM(F.PaidAmount) AS TotalPaidAmount
                               FROM Sale.SaleInvoices SI
                               INNER JOIN Sale.SaleInvoiceFinancialDetails F ON SI.ID = F.InvoiceID
                               WHERE   SI.IsDeleted = 0";

                return await DBHelper.GetAsync<GetSalesInfoOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetSalesInfoOutputDto();
            }
        }

        public async Task<GetInvoiceDetailForPrintOutputDto> GetInvoiceDetailForPrint(GetSaleInvoiceInputDto input)
        {
            try
            {
                GetInvoiceDetailForPrintOutputDto result = new GetInvoiceDetailForPrintOutputDto();
                Dictionary<string, string> dbValues = new Dictionary<string, string>()
                {
                    { "@ID", input.InvoiceID.ToString() },
                };
                string query = string.Empty;
                if (input.IsPrint)
                {
                    query = @"SELECT TOP 1 I.ID, I.InvoiceDate, FORMAT(I.InvoiceDate, 'dd MMM yyyy') +' '+ FORMAT(I.CreatedOn, 'hh:mm tt')
                               AS InvoiceDateFormated,
                               I.Remarks, I.CustomerID, I.CustomerType
                               FROM Sale.SaleInvoices I
                                WHERE I.ID = @ID AND I.IsDeleted = 0 ";
                }
                else
                {
                    query = @"SELECT TOP 1
                           I.ID,  I.InvoiceNo,     I.InvoiceDate,    FORMAT(I.InvoiceDate, 'dd MMM yyyy') + ' ' + FORMAT(I.CreatedOn, 'hh:mm tt') AS InvoiceDateFormated,
                           I.Remarks,  I.CustomerID,  I.CustomerType,
                           (SELECT RC.Name
                              FROM ReferralContact.ReferralContacts RC
                              WHERE CAST(RC.ID AS VARCHAR) = I.ReferredByID ) AS ReferredByName
                        FROM Sale.SaleInvoices I
                        WHERE I.ID = @ID
                          AND I.IsDeleted = 0;";
                }
                result.Invoice = await DBHelper.GetAsync<SaleInvoiceDto>(query, Params: dbValues);

                if (result.Invoice != null && result.Invoice.ID > 0)
                {
                    query = @"SELECT V.ID AS VariationID, V.ItemID, V.VariationName AS Name,II.UnitCost, II.Qty,II.LineTotal, V.Weight, II.DiscountType,II.Discount,
                            WU.Title AS WeightUnit, F.Title AS Formation
                            FROM Sale.SaleInvoiceItems II
                            LEFT JOIN Inventory.InventoryItemVariations V ON V.ID = II.VariationID
                            LEFT JOIN Inventory.ItemAttributes F ON F.ID = V.FormationID AND F.AttributeType = 'Formation'
                            LEFT JOIN Inventory.ItemAttributes WU ON WU.ID = V.WeightUnitID AND WU.AttributeType = 'WeightUnit'
                            WHERE II.InvoiceID = @ID ";

                    result.InvoiceItems = await DBHelper.GetListAsync<SaleInvoiceItemsDto>(query, Params: dbValues);

                    query = @"SELECT FORMAT(Date, 'dd MMM yyyy') Date, PaidAmount, PaymentMode, AttachmentURL
                            FROM Sale.SaleInvoicePayments
                            WHERE InvoiceID = @ID ORDER BY Date ASC";
                    result.InvoicePayments = await DBHelper.GetListAsync<SaleInvoicePaymentsDto>(query, Params: dbValues);

                    query = @"SELECT D.SubTotal, D.DiscountType ,D.DiscountAmount ,D.SubTotalAfterDiscount ,T.Percentage Tax ,D.SubtotalAfterTax,
                            D.PaidAmount ,D.RemainingAmount, T.Title TaxTitle,D.ReturnAmount
                            FROM Sale.SaleInvoiceFinancialDetails D
                            LEFT JOIN Tax.Taxes T ON T.ID = D.TaxID
                            WHERE InvoiceID = @ID";
                    result.InvoiceFinancials = await DBHelper.GetAsync<SaleInvoiceFinancialsDto>(query, Params: dbValues);

                    dbValues.Add("@CustomerID", result.Invoice.CustomerID);
                    if (result.Invoice.CustomerType.Equals("Patient"))
                        query = @"SELECT TOP 1 Name, Phone FROM Patient.Patients WHERE ID = @CustomerID";
                    else
                        query = @"SELECT TOP 1 Name, Phone FROM Patient.Patients WHERE ID = @CustomerID";
                    result.Customer = await DBHelper.GetAsync<SaleInvoiceCustomerDto>(query, Params: dbValues);

                    return result;
                }
                else
                {
                    return new GetInvoiceDetailForPrintOutputDto();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetInvoiceDetailForPrintOutputDto();
            }
        }

        private async Task<string> GetInvoicePrefixAsync()
        {
            // Get invoice prefix for the user
            string invoicePrefixQuery = @"SELECT TOP 1 InvoicePrefix
                                  FROM Invoice.InvoiceSettings ";

            string invoicePrefix = await DBHelper.GetSingleColumnAsync(
                invoicePrefixQuery,
                new Dictionary<string, string>
                {
                });

            if (string.IsNullOrWhiteSpace(invoicePrefix))
                invoicePrefix = "INV";

            // Lock rows to prevent race condition
            string invoiceNoQuery = @"SELECT TOP 1 InvoiceNo
                              FROM Sale.SaleInvoices WITH (UPDLOCK, HOLDLOCK)
                              ORDER BY ID DESC";

            string latestInvoiceNo = await DBHelper.GetSingleColumnAsync(
                invoiceNoQuery,
                new Dictionary<string, string>
                {
                });

            // Generate new invoice number
            string newInvoiceNo;
            if (string.IsNullOrWhiteSpace(latestInvoiceNo))
            {
                newInvoiceNo = $"{invoicePrefix}/1";
            }
            else
            {
                var numberPart = latestInvoiceNo.Replace(invoicePrefix, "").TrimStart('/');
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    newInvoiceNo = $"{invoicePrefix}/{currentNumber + 1}";
                }
                else
                {
                    newInvoiceNo = $"{invoicePrefix}/1";
                }
            }

            return newInvoiceNo;
        }
        public async Task<GetPagedSalesOutputDto> GetLastInvoiceByCustomerAsync(int customerId)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
        {
            { "@CustomerID", customerId.ToString() }
        };

                string query = @"SELECT TOP 1 
                            SI.ID, 
                            SI.InvoiceNo, 
                            SI.InvoiceDate,
                            F.PaidAmount, 
                            F.RemainingAmount, 
                            F.SubtotalAfterTax AS NetTotal,
                            (SELECT C.Name FROM Patient.Patients C WHERE C.ID = SI.CustomerID) AS CustomerName,
                            (SELECT C.Phone FROM Patient.Patients C WHERE C.ID = SI.CustomerID) AS Phone
                         FROM Sale.SaleInvoices SI
                         INNER JOIN Sale.SaleInvoiceFinancialDetails F ON F.InvoiceID = SI.ID
                         WHERE SI.IsDeleted = 0 AND SI.CustomerID = @CustomerID
                         ORDER BY SI.InvoiceDate DESC";

                var results = await DBHelper.GetListAsync<GetPagedSalesOutputDto>(query, null, dbValues);
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }




    }
}