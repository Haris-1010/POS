using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Brandings;
using CoreMedixBeta.Models.Brandings.DTOs;
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
    public class BrandingsRepository
    {
        public async Task<bool> DeleteCustomFieldAsync(DeleteRequestDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() }
                };
                string query = @"DELETE FROM Brandings.PrintBrandingCustomFields WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #region Print Branding

        public async Task<PrintBranding> GetPrintBrandingAsync(GetPrintBrandingInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };
                string query = @"SELECT TOP 1 * FROM Brandings.PrintBrandings";
                return await DBHelper.GetAsync<PrintBranding>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new PrintBranding();
            }
        }

        private const string PrintLogoCacheKey = "CoreMedixBeta.PrintBranding.Logo";

        /// <summary>
        /// Print Branding ka logo URL (favicon ke liye). 5 minute tak cache rehta hai.
        /// </summary>
        public static string GetPrintLogoUrl()
        {
            try
            {
                string cached = HttpRuntime.Cache[PrintLogoCacheKey] as string;
                if (cached != null) return cached;

                var data = Task.Run(() => new BrandingsRepository()
                    .GetPrintBrandingAsync(new GetPrintBrandingInputDto()))
                    .GetAwaiter()
                    .GetResult();

                string logo = (data != null && !string.IsNullOrWhiteSpace(data.Logo)) ? data.Logo.Trim() : string.Empty;

                if (logo.Length > 0
                    && !logo.StartsWith("/", StringComparison.Ordinal)
                    && !logo.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    && !logo.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    logo = "/" + logo;
                }

                HttpRuntime.Cache.Add(
                    PrintLogoCacheKey,
                    logo,
                    null,
                    DateTime.Now.AddMinutes(5),
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Normal,
                    null);

                return logo;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static void ClearPrintLogoCache()
        {
            try { HttpRuntime.Cache.Remove(PrintLogoCacheKey); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private const string BusinessNameCacheKey = "CoreMedixBeta.PrintBranding.BusinessName";

        /// <summary>
        /// Branding ka Business Name (top-left brand ke liye). 5 minute tak cache rehta hai.
        /// </summary>
        public static string GetBusinessName()
        {
            try
            {
                string cached = HttpRuntime.Cache[BusinessNameCacheKey] as string;
                if (cached != null) return cached;

                var data = Task.Run(() => new BrandingsRepository()
                    .GetPrintBrandingAsync(new GetPrintBrandingInputDto()))
                    .GetAwaiter()
                    .GetResult();

                string name = (data != null && !string.IsNullOrWhiteSpace(data.BusinessName))
                    ? data.BusinessName.Trim()
                    : string.Empty;

                HttpRuntime.Cache.Add(
                    BusinessNameCacheKey,
                    name,
                    null,
                    DateTime.Now.AddMinutes(5),
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Normal,
                    null);

                return name;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public async Task<Tuple<string, string>> GetPrintBrandingCustomFieldListAsync(PagedRequestInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT PBCF.ID, PBCF.FieldName, PBCF.FieldValue, PBCF.ForModule,
                       PBCF.InVisible, PBCF.[Order], PB.ID AS BrandingID
                       FROM Brandings.PrintBrandingCustomFields PBCF
                       INNER JOIN Brandings.PrintBrandings PB ON PBCF.BrandingId = PB.ID";

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (Name FieldName @SearchString ) ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var data = await DBHelper.GetPagedListAsync<GetPrintBrandingCustomFieldOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<List<GetPrintBrandingCustomFieldOutputDto>> GetPrintBrandingCustomFieldShortListAsync(RequestByMasterUsernameDTO input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT PBCF.ID, PBCF.FieldName, PBCF.FieldValue, PBCF.ForModule,
                       PBCF.InVisible, PBCF.[Order],PB.ID AS BrandingID
                       FROM Brandings.PrintBrandingCustomFields PBCF
                       INNER JOIN Brandings.PrintBrandings PB ON PBCF.BrandingId = PB.ID
                       and PBCF.InVisible = 1 ORDER BY PBCF.[Order] ASC ";

                return await DBHelper.GetListAsync<GetPrintBrandingCustomFieldOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetPrintBrandingCustomFieldOutputDto>();
            }
        }

        public async Task<PrintBranding> SavePrintBrandingAsync(PrintBranding input)
        {
            try
            {
                input.LastModifiedOn = ServerConfig.DateTimeNow;
                if (input.ID > 0)
                {
                    await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    });
                }
                else
                {
                    int insertedID = int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                    if (insertedID > 0)
                    {
                        input.ID = insertedID;
                    }
                }

                // Taake naya business name turant layout par show ho
                try { HttpRuntime.Cache.Remove(BusinessNameCacheKey); } catch { }

                return input;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new PrintBranding();
            }
        }

        public async Task<PrintBrandingCustomField> SavePrintBrandingCustomFieldAsync(PrintBrandingCustomField input)
        {
            try
            {
                if (input.ID > 0)
                {
                    await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    });
                }
                else
                {
                    int insertedID = int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                    if (insertedID > 0)
                    {
                        input.ID = insertedID;
                    }
                }
                return input;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new PrintBrandingCustomField();
            }
        }

        #endregion Print Branding
    }
}