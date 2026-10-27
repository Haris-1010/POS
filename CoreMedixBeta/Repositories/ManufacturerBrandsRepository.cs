using CoreMedixBeta.Models;
using CoreMedixBeta.Models.ManufacturerBrand;
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
    public class ManufacturerBrandsRepository
    {
        public async Task<bool> SaveBrand(ManufacturerBrands input)
        {
            try
            {
                bool IsSaved = false;
                if (input.ID == 0)
                {
                    IsSaved = await DBHelper.InsertAsync(input);
                }
                else
                {
                    IsSaved = await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    });
                }

                return IsSaved;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteBrand(DeleteBrandInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.ID.ToString() }
                };

                string query = @"DELETE FROM Manufacturer.ManufacturerBrands WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Tuple<string, string>> GetPagedBrand(GetPagedBrandsInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ManufacturerID", input.ManufacturerID.ToString() }
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = $@"SELECT ID, Title FROM Manufacturer.ManufacturerBrands WHERE ManufacturerID = @ManufacturerID ";
                string countQuery = $@"SELECT COUNT(*) FROM Manufacturer.ManufacturerBrands WHERE ManufacturerID = @ManufacturerID ";

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (Title LIKE @SearchString) ";
                    countQuery += " AND (Title LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var temp = await DBHelper.GetPagedListAsync<ManufacturerBrands>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);
                var data = temp.Results;
                string jsonData = JsonConvert.SerializeObject(data);
                string totalRecords = JsonConvert.SerializeObject(temp.pager.TotalRecords);

                return Tuple.Create(jsonData, totalRecords);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<List<ManufacturerBrands>> GetBrandShortList(GetPagedBrandsInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ManufacturerID", input.ManufacturerID.ToString() },
                };

                string query = @"SELECT ID, Title FROM Manufacturer.ManufacturerBrands WHERE ManufacturerID = @ManufacturerID ";
                return await DBHelper.GetListAsync<ManufacturerBrands>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ManufacturerBrands>();
            }
        }

        public async Task<List<ManufacturerBrands>> GetBrandsByMasterUsernameShortList(GetBrandShortListInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT B.ID, B.Title FROM Manufacturer.ManufacturerBrands B
                               INNER JOIN Manufacturer.Manufacturers M ON M.ID = B.ManufacturerID ";
                return await DBHelper.GetListAsync<ManufacturerBrands>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ManufacturerBrands>();
            }
        }
    }
}