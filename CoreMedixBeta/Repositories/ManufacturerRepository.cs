using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Models.Menufacturer;
using CoreMedixBeta.Models.Menufacturer.DTOs;
using CoreMedixBeta.Models.Patient;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static CoreMedixBeta.Repositories.ManufacturerRepository;

namespace CoreMedixBeta.Repositories
{
    public class ManufacturerRepository
    {
        public async Task<bool> DeleteManufacturerAsync(DeleteManufacturerInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                    {"@DeletedBy", input.DeletedBy },
                    {"@DeletedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                };
                string query = @"UPDATE Manufacturer.Manufacturers SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<int> SaveManufacturerAsync(Manufacturer input)
        {
            try
            {
                if (input.ID == 0)
                {
                    input.CreatedOn = DateTime.Now;
                    return int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                }
                else
                {
                    input.LastModifiedOn = DateTime.Now;
                    bool IsUpdated = await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    });

                    if (IsUpdated) return input.ID;
                    else return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<Tuple<string, string>> GetPagedManufacturersAsync(GetPagedManufacturersInputDto input)
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
            };

            DataTableValuesDTO dataTableValues = input.DataTableValues;

            try
            {
                string query = $@"SELECT ID, Name, Phone, City, Email
                               FROM Manufacturer.Manufacturers
                               WHERE IsDeleted = 0 ";

                string countQuery = $@"SELECT COUNT(*) FROM Manufacturer.Manufacturers WHERE  IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE, CreatedOn) BETWEEN CONVERT(DATE,@StartDate) AND CONVERT(DATE,@EndDate) ";
                    countQuery += " AND CONVERT(DATE, CreatedOn) BETWEEN CONVERT(DATE,@StartDate) AND CONVERT(DATE,@EndDate) ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (Name LIKE @SearchString OR Phone LIKE @SearchString OR Email LIKE @SearchString ) ";
                    countQuery += " AND (Name LIKE @SearchString OR Phone LIKE @SearchString OR Email LIKE @SearchString ) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var temp = await DBHelper.GetPagedListAsync<GetPagedManufacturersOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);
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

        public async Task<List<GetManufacturerShortListOutputDto>> GetManufacturerShortList()
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
            };

            try
            {
                string query = @"SELECT ID, Name FROM Manufacturer.Manufacturers
                               WHERE  IsDeleted = 0";
                return await DBHelper.GetListAsync<GetManufacturerShortListOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetManufacturerShortListOutputDto>();
            }
        }

        public async Task<List<GetManufacturerShortListOutputDto>> SearchManufacturerAsync(SearchManufacturerInputDto input)
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
                { "@SearchString", $"%{input.SearchString}%" },
            };

            try
            {
                string query = @"SELECT ID, Name FROM Manufacturer.Manufacturers
                               WHERE  Name LIKE @SearchString AND IsDeleted = 0";
                return await DBHelper.GetListAsync<GetManufacturerShortListOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetManufacturerShortListOutputDto>();
            }
        }
    }
}