using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Menufacturer.DTOs;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Supplier;
using CoreMedixBeta.Models.Supplier.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class SupplierRepository
    {
        public async Task<int> SaveSupplierAsync(Supplier input)
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

        public async Task<List<GetSupplierShortListOutputDto>> GetSupplierShortList()
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
            };

            try
            {
                string query = @"SELECT ID, Name, Phone FROM Supplier.Suppliers
                         WHERE   IsDeleted = 0";
                return await DBHelper.GetListAsync<GetSupplierShortListOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetSupplierShortListOutputDto>();
            }
        }

        public async Task<bool> DeleteSupplierAsync(DeleteSupplierInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.SupplierID.ToString() },
                    { "@DeletedBy", input.DeletedBy },
                    { "@DeletedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                string query = @"UPDATE Supplier.Suppliers SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Tuple<string, string>> GetPagedSuppliersAsync(GetPagedSuppliersInputDto input)
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
            };

            DataTableValuesDTO dataTableValues = input.DataTableValues;

            try
            {
                string query = $@"SELECT ID, Name, Phone, City, Email
                  FROM Supplier.Suppliers
                  WHERE IsDeleted = 0 ";

                string countQuery = $@"SELECT COUNT(*) FROM Supplier.Suppliers WHERE   IsDeleted = 0 ";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));

                    query += " AND CONVERT(DATE, CreatedOn) BETWEEN CONVERT(DATE, @StartDate) AND CONVERT(DATE, @EndDate) ";
                    countQuery += " AND CONVERT(DATE, CreatedOn) BETWEEN CONVERT(DATE, @StartDate) AND CONVERT(DATE, @EndDate) ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (Name LIKE @SearchString OR Phone LIKE @SearchString OR Email LIKE @SearchString) ";
                    countQuery += " AND (Name LIKE @SearchString OR Phone LIKE @SearchString OR Email LIKE @SearchString) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var temp = await DBHelper.GetPagedListAsync<GetPagedManufacturersOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);
                var data = temp.Results;
                string jsonData = JsonConvert.SerializeObject(data);
                string totalRecords = JsonConvert.SerializeObject(temp.pager.TotalRecords);

                return Tuple.Create(jsonData, totalRecords);
            }
            catch (Exception)
            {
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<List<GetSupplierShortListOutputDto>> GetSupplierShortList(Supplier input)
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
            };

            try
            {
                string query = @"SELECT ID, Name, Phone FROM Supplier.Suppliers
                 WHERE  IsDeleted = 0";
                return await DBHelper.GetListAsync<GetSupplierShortListOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetSupplierShortListOutputDto>();
            }
        }

        public async Task<List<GetSupplierShortListOutputDto>> SearchSupplierAsync(SearchSupplierInputDto input)
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
                { "@SearchString", $"%{input.SearchString}%" }
            };

            try
            {
                string query = @"SELECT ID, Name, Phone FROM Supplier.Suppliers
                       WHERE   Name LIKE @SearchString AND IsDeleted = 0";
                return await DBHelper.GetListAsync<GetSupplierShortListOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetSupplierShortListOutputDto>();
            }
        }
    }
}