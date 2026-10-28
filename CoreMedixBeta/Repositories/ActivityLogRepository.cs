using CoreMedixBeta.Models;
using CoreMedixBeta.Models.Audit;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Reports.DTOs;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreMedixBeta.Repositories
{
    public class ActivityLogRepository
    {
        public async Task LogAsync(string username, string action, string entityType, string entityId, string details, string ipAddress)
        {
            try
            {
                var log = new ActivityLog
                {
                    Username = Truncate(username, 100),
                    Action = Truncate(action, 50),
                    EntityType = Truncate(entityType, 100),
                    EntityID = Truncate(entityId, 100),
                    Details = Truncate(details, 1000),
                    IPAddress = Truncate(ipAddress, 50)
                };
                await DBHelper.InsertAsync(log);
            }
            catch
            {
                // Audit logging must never break the main operation
            }
        }

        public async Task<Tuple<List<ActivityLog>, int>> GetPagedAsync(int start, int length, string search, string action, string entityType, string username)
        {
            var dbValues = new Dictionary<string, string>
            {
                { "@Start", (start + 1).ToString() },
                { "@Length", length.ToString() }
            };

            string whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                whereClause += " AND (Username LIKE @Search OR Details LIKE @Search OR EntityID LIKE @Search) ";
                dbValues.Add("@Search", $"%{search}%");
            }
            if (!string.IsNullOrWhiteSpace(action))
            {
                whereClause += " AND Action = @Action ";
                dbValues.Add("@Action", action);
            }
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                whereClause += " AND EntityType = @EntityType ";
                dbValues.Add("@EntityType", entityType);
            }
            if (!string.IsNullOrWhiteSpace(username))
            {
                whereClause += " AND Username = @Username ";
                dbValues.Add("@Username", username);
            }

            string countQuery = $"SELECT COUNT(*) FROM dbo.ActivityLogs {whereClause}";
            int totalRecords = await DBHelper.GetScalerAsync(countQuery, dbValues);

            string query = $@"
                WITH Ordered AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY CreatedOn DESC, ID DESC) AS RowNum
                    FROM dbo.ActivityLogs
                    {whereClause}
                )
                SELECT ID, Username, Action, EntityType, EntityID, Details, IPAddress, CreatedOn
                FROM Ordered
                WHERE RowNum BETWEEN @Start AND @Start + @Length - 1
                ORDER BY CreatedOn DESC, ID DESC";

            var logs = await DBHelper.GetListAsync<ActivityLog>(query, Params: dbValues) ?? new List<ActivityLog>();
            return Tuple.Create(logs, totalRecords);
        }

        public async Task<Tuple<string, string>> GetPagedAuditLogsAsync(GetPagedAuditLogsInputDto input)
        {
            try
            {
                var dbValues = new Dictionary<string, string>();
                DataTableValuesDTO dataTableValues = input.DataTableValues ?? new DataTableValuesDTO();

                string query = @"SELECT ID, Username, Action, EntityType, EntityID, Details, IPAddress, CreatedOn
                                 FROM dbo.ActivityLogs
                                 WHERE 1 = 1";

                if (!string.IsNullOrEmpty(input.StartDate) && !string.IsNullOrEmpty(input.EndDate))
                {
                    dbValues.Add("@StartDate", Convert.ToDateTime(input.StartDate).ToString("yyyy-MM-dd"));
                    dbValues.Add("@EndDate", Convert.ToDateTime(input.EndDate).ToString("yyyy-MM-dd"));
                    query += " AND CONVERT(DATE, CreatedOn) BETWEEN @StartDate AND @EndDate ";
                }
                if (!string.IsNullOrWhiteSpace(input.Action))
                {
                    dbValues.Add("@Action", input.Action);
                    query += " AND Action = @Action ";
                }
                if (!string.IsNullOrWhiteSpace(input.Username))
                {
                    dbValues.Add("@Username", input.Username);
                    query += " AND Username = @Username ";
                }
                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@Search", $"%{dataTableValues.SearchString}%");
                    query += " AND (Username LIKE @Search OR Details LIKE @Search OR EntityID LIKE @Search OR Action LIKE @Search OR EntityType LIKE @Search OR IPAddress LIKE @Search) ";
                }

                string sortBy = "CreatedOn";
                var allowedSortColumns = new[] { "CreatedOn", "Username", "Action", "EntityType" };
                if (!string.IsNullOrEmpty(input.SortBy) && allowedSortColumns.Contains(input.SortBy))
                {
                    sortBy = input.SortBy;
                }
                string sortOrder = string.Equals(input.SortByOrder, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

                var sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList." + sortBy, sortOrder }
                };

                int page = dataTableValues.PageSize > 0 ? dataTableValues.Offset / dataTableValues.PageSize : 0;
                var temp = await DBHelper.GetPagedListAsync<GetPagedAuditLogsOutputDto>(query, sortAndOrder, dataTableValues.PageSize, page, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(temp.Results), JsonConvert.SerializeObject(temp.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<List<string>> GetDistinctActionsAsync()
        {
            var rows = await DBHelper.GetListAsync<ActivityLog>(
                "SELECT DISTINCT Action FROM dbo.ActivityLogs WHERE ISNULL(Action, '') <> '' ORDER BY Action",
                Params: new Dictionary<string, string>());
            return rows?.Select(r => r.Action).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        }

        public async Task<List<string>> GetDistinctUsernamesAsync()
        {
            var rows = await DBHelper.GetListAsync<ActivityLog>(
                "SELECT DISTINCT Username FROM dbo.ActivityLogs WHERE ISNULL(Username, '') <> '' ORDER BY Username",
                Params: new Dictionary<string, string>());
            return rows?.Select(r => r.Username).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
