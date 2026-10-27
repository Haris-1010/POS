using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.ReferralOrders.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class ReferralOrdersRepository
    {
        public async Task<int> CreateOrderAsync(CreateOrderDto input)
        {
            try
            {
                int orderID = int.Parse(await DBHelper.InsertAndGetIdAsync(input.Order));
                if (orderID == 0) return 0;

                try
                {
                    input.OrderItems.ForEach(item => item.OrderID = orderID);
                    await DBHelper.InsertListAsync(input.OrderItems);
                    return orderID;
                }
                catch (Exception)
                {
                    await DBHelper.ExecuteQueryAsync(
                        "DELETE FROM ReferralOrder.ReferralOrders WHERE ID = @ID",
                        new Dictionary<string, string> { { "@ID", orderID.ToString() } }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }

        public async Task<Tuple<string, string>> GetPagedOrdersAsync(GetPagedOrdersInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT
                       O.ID,
                       O.PrescriptionID,                    P.Name AS PatientName, P.Phone AS PatientPhone, P.MrNumber AS MR_No,                    O.PatientUsername, O.Source, O.OrderOn,   O.PrescriptionStyleID, O.ReferredByID,
                       O.ReferredByType,  (SELECT RC.Name
                          FROM ReferralContact.ReferralContacts RC
                          WHERE CAST(RC.ID AS VARCHAR) = O.ReferredByID
                       ) AS ReferredByName,                    O.IsConversionStarted, O.ConversionStartedOn, O.IsConvertedToInvoice, O.ConvertedToInvoiceOn, O.IsOrderCancelled, O.OrderCancelledOn
                FROM ReferralOrder.ReferralOrders O
                INNER JOIN Patient.Patients P ON O.PatientUsername = P.Name;
                 ";

                if (!string.IsNullOrWhiteSpace(input.ReferredByID) && !string.IsNullOrWhiteSpace(input.ReferredByType))
                {
                    dbValues.Add("@ReferredByID", input.ReferredByID);
                    dbValues.Add("@ReferredByType", input.ReferredByType);
                    query += " AND (O.ReferredByID = @ReferredByID AND O.ReferredByType = @ReferredByType) ";
                }

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE,O.OrderOn) BETWEEN @StartDate AND @EndDate ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (P.Name LIKE @SearchString OR P.Phone LIKE @SearchString )  ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
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

                var temp = await DBHelper.GetPagedListAsync<GetPagedOrdersOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
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