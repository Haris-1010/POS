using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Patient.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreMedixBeta.Repositories
{
    public class PatientRepository
    {
        public async Task<List<Patients>> GetAllPatientsAsync()
        {
            return await DBHelper.GetListAsync<Patients>();
        }

        public async Task<List<string>> GetAllCitiesNamesAsync()
        {
            try
            {
                string query = "SELECT Name From Cities ORDER BY Name";
                return await DBHelper.QueryColumnAsync(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }

        public async Task<int> SavePatientsAsync(Patients patients)
        {
            try
            {
                if (patients == null || string.IsNullOrWhiteSpace(patients.Name)) return 0;

                // Quick patient: sirf Name required hai, baaki sab optional.
                patients.Name = patients.Name.Trim();
                if (patients.Phone == null) patients.Phone = string.Empty;
                if (string.IsNullOrWhiteSpace(patients.DOB)) patients.DOB = null;

                if (patients.ID == 0)
                {
                    if (string.IsNullOrWhiteSpace(patients.CreatedBy)) patients.CreatedBy = "System";
                    patients.CreatedOn = DateTime.Now;
                    patients.PatientTypeID = 1;
                    var result = await DBHelper.InsertAndGetIdAsync(patients);

                    if (int.TryParse(result, out int newId))
                    {
                        return newId;
                    }
                    else
                    {
                        Console.WriteLine($"InsertAndGetIdAsync returned invalid ID: {result}");
                        return 0;
                    }
                }
                else
                {
                    patients.LastModifiedOn = DateTime.Now;
                    if (string.IsNullOrWhiteSpace(patients.LastModifiedBy)) patients.LastModifiedBy = "System";
                    patients.PatientTypeID = 1;
                    bool IsUpdated = await DBHelper.UpdateAsync(patients, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", patients.ID.ToString() }
                    });

                    if (IsUpdated) return patients.ID;
                    else return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<Tuple<string, string>> GetPagedPatientAsync(GetPagedPatientInputDto input)
        {
            Dictionary<string, string> dbValues = new Dictionary<string, string>
            {
            };

            DataTableValuesDTO dataTableValues = input.DataTableValues;

            try
            {
               
                string query = $@"SELECT ID, Name, Phone, City, CNIC, MR_No,Address,FORMAT(DOB, 'dd MMM yyyy') AS DOB,Gender
                   FROM Patient.Patients WHERE IsDeleted = 0 ";

                string countQuery = $@"SELECT COUNT(*) FROM Patient.Patients WHERE IsDeleted = 0 ";

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
                    query += " AND (Name LIKE @SearchString OR Phone LIKE @SearchString OR CNIC LIKE @SearchString ) ";
                    countQuery += " AND (Name LIKE @SearchString OR Phone LIKE @SearchString OR CNIC LIKE @SearchString ) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var data = await DBHelper.GetPagedListAsync<Patients>(
                    query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);

                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<bool> DeletePatientAsync(DeletePatientInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                    {"@DeletedBy", input.DeletedBy },
                    {"@DeletedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                };
                string query = @"UPDATE Patient.Patients SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<SearchPatientOutputDto>> SearchCustomerAsync(SearchPatientInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>()
                {
                    { "@SearchString", $"%{input.SearchString}%" }
                };

                            string query = @"
                          SELECT TOP 8 
                   MP.ID AS CustomerID, 
                   MP.Name AS CustomerName, 
                   MP.Phone, 
                   'Patient' AS CustomerType
            FROM Patient.Patients MP
            WHERE MP.IsDeleted = 0
              AND (MP.Name LIKE @SearchString OR MP.Phone LIKE @SearchString);
            ";

                return await DBHelper.GetListAsync<SearchPatientOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<SearchPatientOutputDto>();
            }
        }

        public async Task<int> FindPatientIdByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return 0;

                Dictionary<string, string> dbValues = new Dictionary<string, string>()
                {
                    { "@Name", name.Trim() }
                };

                string query = @"
                    SELECT TOP 1 MP.ID
                    FROM Patient.Patients MP
                    WHERE MP.IsDeleted = 0
                      AND LTRIM(RTRIM(MP.Name)) = @Name
                    ORDER BY MP.ID DESC;";

                return await DBHelper.GetScalerAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
    }
}
