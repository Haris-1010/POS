using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Discounts;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.Patient;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class DiscountsRepository
    {
        public async Task<bool> DeleteDiscountAsync(DeleteDiscountInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() }
                };
                string query = @"DELETE FROM Discount.Discounts WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<GetDiscountsShortOutputDto>> GetDiscountsShortListAsync(RequestByMasterUsernameDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = $@"SELECT ID, DiscountName, DiscountType, DiscountValue, IsActive
                               FROM Discount.Discounts
                               WHERE  IsActive = 1";
                return await DBHelper.GetListAsync<GetDiscountsShortOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetDiscountsShortOutputDto>();
            }
        }

        public async Task<Tuple<string, string>> GetPagedDiscountsAsync(PagedRequestInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = $@"SELECT ID, DiscountName, DiscountType, DiscountValue, IsActive
                               FROM Discount.Discounts ";

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += "WHERE DiscountName LIKE '%' + @SearchString + '%'";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var data = await DBHelper.GetPagedListAsync<GetDiscountsShortOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<int> SaveDiscountAsync(Discount input)
        {
            try
            {
                if (input.ID == 0)
                {
                    input.TimeStamp = ServerConfig.DateTimeNow;
                    return int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                }
                else
                {
                    if (await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    })) return input.ID;
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