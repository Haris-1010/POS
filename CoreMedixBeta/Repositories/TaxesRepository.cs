using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Taxes;
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
    public class TaxesRepository
    {
        public async Task<bool> DeleteTaxAsync(DeleteTaxInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                };
                string query = @"DELETE FROM  Tax.Taxes WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<Tuple<string, string>> GetPagedTaxesAsync(GetPagedTaxesInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT ID, Title, Percentage,PaymentMode FROM Tax.Taxes ";

                string countQuery = @"SELECT COUNT(*) FROM Tax.Taxes ";

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };
                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += "WHERE Title LIKE '%' + @SearchString + '%'";
                }
                var temp = await DBHelper.GetPagedListAsync<GetPagedTaxesOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);
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

        public async Task<List<TaxesShortListOutputDto>> GetTaxesShortListAsync(TaxesShortListInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT ID, Title, Percentage, PaymentMode FROM Tax.Taxes
                              ORDER BY Percentage ASC";

                return await DBHelper.GetListAsync<TaxesShortListOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<TaxesShortListOutputDto>();
            }
        }

        public async Task<int> SaveTaxesAsync(Tax input)
        {
            try
            {
                if (input.ID == 0)
                {
                    return int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                }
                else
                {
                    input.LastModifiedOn = ServerConfig.DateTimeNow;
                    bool IsUpdated = await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    });
                    if (IsUpdated) return input.ID;
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
    }
}