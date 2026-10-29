using CoreMedixBeta.Models.BackupSettings;
using CoreMedixBeta.Models.BackupSettings.DTOs;
using CoreMedixBeta.Models.Patient;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreMedixBeta.Repositories
{
    public class BackupSettingsRepository
    {
        public async Task<BackupSetting> GetSettingsAsync()
        {
            try
            {
                string query = @"SELECT TOP 1 ID, IsEnabled, FrequencyMinutes, BackupFolderPath, RetentionDays,
                                        LastBackupTime, NextBackupTime, LastStatus, LastErrorMessage,
                                        LastModifiedBy, LastModifiedOn
                                 FROM Settings.BackupSettings";

                BackupSetting settings = await DBHelper.GetAsync<BackupSetting>(query);

                if (settings == null || settings.ID == 0)
                {
                    return await CreateDefaultSettingsAsync();
                }

                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new BackupSetting();
            }
        }

        public async Task<BackupSetting> SaveSettingsAsync(BackupSetting input)
        {
            try
            {
                input.LastModifiedOn = DateTime.Now;

                if (input.ID == 0)
                {
                    string insertQuery = @"INSERT INTO Settings.BackupSettings
                                           (IsEnabled, FrequencyMinutes, BackupFolderPath, RetentionDays,
                                            LastBackupTime, NextBackupTime, LastStatus, LastErrorMessage,
                                            LastModifiedBy, LastModifiedOn)
                                           VALUES (@IsEnabled, @FrequencyMinutes, @BackupFolderPath, @RetentionDays,
                                                   @LastBackupTime, @NextBackupTime, @LastStatus, @LastErrorMessage,
                                                   @LastModifiedBy, @LastModifiedOn);
                                           SELECT SCOPE_IDENTITY();";

                    string newId = await DBHelper.GetSingleColumnAsync(insertQuery, BuildSettingsParams(input));
                    if (int.TryParse(newId, out int id)) input.ID = id;
                    return input;
                }
                else
                {
                    string updateQuery = @"UPDATE Settings.BackupSettings SET
                                               IsEnabled = @IsEnabled,
                                               FrequencyMinutes = @FrequencyMinutes,
                                               BackupFolderPath = @BackupFolderPath,
                                               RetentionDays = @RetentionDays,
                                               LastModifiedBy = @LastModifiedBy,
                                               LastModifiedOn = @LastModifiedOn
                                           WHERE ID = @ID";

                    await DBHelper.ExecuteQueryAsync(updateQuery, BuildSettingsParams(input));
                    return input;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<bool> UpdateScheduleStateAsync(int id, DateTime? lastBackupTime, DateTime? nextBackupTime, string lastStatus, string lastErrorMessage)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", id.ToString() },
                    { "@LastBackupTime", lastBackupTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@NextBackupTime", nextBackupTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@LastStatus", string.IsNullOrEmpty(lastStatus) ? null : lastStatus },
                    { "@LastErrorMessage", string.IsNullOrEmpty(lastErrorMessage) ? null : lastErrorMessage }
                };

                string query = @"UPDATE Settings.BackupSettings SET
                                     LastBackupTime = @LastBackupTime,
                                     NextBackupTime = @NextBackupTime,
                                     LastStatus = @LastStatus,
                                     LastErrorMessage = @LastErrorMessage
                                 WHERE ID = @ID";

                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<int> InsertHistoryEntryAsync(BackupHistoryEntry entry)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@BackupFileName", entry.BackupFileName },
                    { "@BackupFilePath", entry.BackupFilePath },
                    { "@BackupStartTime", entry.BackupStartTime.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@Status", entry.Status },
                    { "@TriggerType", entry.TriggerType },
                    { "@CreatedBy", string.IsNullOrEmpty(entry.CreatedBy) ? null : entry.CreatedBy }
                };

                string query = @"INSERT INTO Settings.BackupHistory
                                 (BackupFileName, BackupFilePath, BackupStartTime, Status, TriggerType, CreatedBy)
                                 VALUES (@BackupFileName, @BackupFilePath, @BackupStartTime, @Status, @TriggerType, @CreatedBy);
                                 SELECT SCOPE_IDENTITY();";

                string newId = await DBHelper.GetSingleColumnAsync(query, dbValues);
                return int.TryParse(newId, out int id) ? id : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<bool> UpdateHistoryResultAsync(int id, DateTime endTime, long? fileSizeBytes, string status, string errorMessage)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", id.ToString() },
                    { "@BackupEndTime", endTime.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@FileSizeBytes", fileSizeBytes?.ToString() },
                    { "@Status", status },
                    { "@ErrorMessage", string.IsNullOrEmpty(errorMessage) ? null : errorMessage }
                };

                string query = @"UPDATE Settings.BackupHistory SET
                                     BackupEndTime = @BackupEndTime,
                                     FileSizeBytes = @FileSizeBytes,
                                     Status = @Status,
                                     ErrorMessage = @ErrorMessage
                                 WHERE ID = @ID";

                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> MarkHistoryDeletedAsync(int id, string reason)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", id.ToString() },
                    { "@Reason", reason }
                };

                string query = @"UPDATE Settings.BackupHistory SET
                                     Status = 'Deleted',
                                     ErrorMessage = @Reason
                                 WHERE ID = @ID AND Status <> 'Deleted'";

                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<BackupHistoryEntry> GetHistoryByIdAsync(int id)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", id.ToString() }
                };

                string query = @"SELECT ID, BackupFileName, BackupFilePath, BackupStartTime, BackupEndTime,
                                        FileSizeBytes, Status, ErrorMessage, TriggerType, CreatedBy
                                 FROM Settings.BackupHistory
                                 WHERE ID = @ID";

                return await DBHelper.GetAsync<BackupHistoryEntry>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<BackupHistoryEntry> GetLatestSuccessAsync()
        {
            try
            {
                string query = @"SELECT TOP 1 ID, BackupFileName, BackupFilePath, BackupStartTime, BackupEndTime,
                                        FileSizeBytes, Status, ErrorMessage, TriggerType, CreatedBy
                                 FROM Settings.BackupHistory
                                 WHERE Status = 'Success'
                                 ORDER BY BackupStartTime DESC";

                return await DBHelper.GetAsync<BackupHistoryEntry>(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<List<BackupHistoryEntry>> GetExpiredSuccessBackupsAsync(DateTime cutoff)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@Cutoff", cutoff.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                string query = @"SELECT ID, BackupFileName, BackupFilePath, BackupStartTime, BackupEndTime,
                                        FileSizeBytes, Status, ErrorMessage, TriggerType, CreatedBy
                                 FROM Settings.BackupHistory
                                 WHERE Status = 'Success' AND BackupStartTime < @Cutoff
                                 ORDER BY BackupStartTime DESC";

                return await DBHelper.GetListAsync<BackupHistoryEntry>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<BackupHistoryEntry>();
            }
        }

        public async Task<Tuple<string, string>> GetPagedBackupHistoryAsync(GetPagedBackupHistoryInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string> { };

                DataTableValuesDTO dataTableValues = input.DataTableValues ?? new DataTableValuesDTO();

                string query = @"SELECT ID, BackupFileName,
                                        CONVERT(NVARCHAR(19), BackupStartTime, 120) AS BackupStartTime,
                                        CONVERT(NVARCHAR(19), BackupEndTime, 120) AS BackupEndTime,
                                        FileSizeBytes,
                                        CASE
                                            WHEN FileSizeBytes IS NULL THEN '-'
                                            WHEN FileSizeBytes >= 1073741824 THEN CAST(CAST(FileSizeBytes / 1073741824.0 AS DECIMAL(18, 2)) AS NVARCHAR(20)) + ' GB'
                                            WHEN FileSizeBytes >= 1048576 THEN CAST(CAST(FileSizeBytes / 1048576.0 AS DECIMAL(18, 2)) AS NVARCHAR(20)) + ' MB'
                                            WHEN FileSizeBytes >= 1024 THEN CAST(CAST(FileSizeBytes / 1024.0 AS DECIMAL(18, 2)) AS NVARCHAR(20)) + ' KB'
                                            ELSE CAST(FileSizeBytes AS NVARCHAR(20)) + ' B'
                                        END AS FileSize,
                                        Status, TriggerType, CreatedBy, ErrorMessage
                                 FROM Settings.BackupHistory ";

                string countQuery = @"SELECT COUNT(*) FROM Settings.BackupHistory ";

                if (dataTableValues.PageSize <= 0) dataTableValues.PageSize = 10;
                int p = dataTableValues.Offset / dataTableValues.PageSize;

                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += "WHERE BackupFileName LIKE @SearchString ";
                    countQuery += "WHERE BackupFileName LIKE @SearchString ";
                }

                var temp = await DBHelper.GetPagedListAsync<GetPagedBackupHistoryOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);

                string jr = JsonConvert.SerializeObject(temp.Results);
                string total = JsonConvert.SerializeObject(temp.pager.TotalRecords);

                return Tuple.Create(jr, total);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create("[]", "0");
            }
        }

        public async Task<bool> InsertAuditLogAsync(string activityType, string status, string details, string performedBy)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ActivityType", activityType },
                    { "@Status", status },
                    { "@Details", string.IsNullOrEmpty(details) ? null : details },
                    { "@PerformedBy", string.IsNullOrEmpty(performedBy) ? null : performedBy },
                    { "@PerformedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                string query = @"INSERT INTO Settings.BackupAuditLog (ActivityType, Status, Details, PerformedBy, PerformedOn)
                                 VALUES (@ActivityType, @Status, @Details, @PerformedBy, @PerformedOn)";

                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<string> GetDatabaseNameAsync()
        {
            try
            {
                return await DBHelper.GetSingleColumnAsync("SELECT DB_NAME()");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private async Task<BackupSetting> CreateDefaultSettingsAsync()
        {
            try
            {
                BackupSetting defaults = new BackupSetting
                {
                    IsEnabled = false,
                    FrequencyMinutes = 1440,
                    BackupFolderPath = GetDefaultBackupFolder(),
                    RetentionDays = 30,
                    LastStatus = "Disabled",
                    LastModifiedOn = DateTime.Now
                };

                string insertQuery = @"INSERT INTO Settings.BackupSettings
                                       (IsEnabled, FrequencyMinutes, BackupFolderPath, RetentionDays, LastStatus, LastModifiedOn)
                                       VALUES (@IsEnabled, @FrequencyMinutes, @BackupFolderPath, @RetentionDays, @LastStatus, @LastModifiedOn);
                                       SELECT SCOPE_IDENTITY();";

                string newId = await DBHelper.GetSingleColumnAsync(insertQuery, BuildSettingsParams(defaults));
                if (int.TryParse(newId, out int id)) defaults.ID = id;

                return defaults;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new BackupSetting
                {
                    BackupFolderPath = GetDefaultBackupFolder(),
                    FrequencyMinutes = 1440,
                    RetentionDays = 30,
                    LastStatus = "Disabled"
                };
            }
        }

        public static string GetDefaultBackupFolder()
        {
            try
            {
                string root = System.IO.Path.GetPathRoot(Environment.SystemDirectory);
                if (string.IsNullOrWhiteSpace(root)) root = @"C:\";
                return System.IO.Path.Combine(root, "POS_Backups");
            }
            catch
            {
                return @"C:\POS_Backups";
            }
        }

        private static Dictionary<string, string> BuildSettingsParams(BackupSetting input)
        {
            return new Dictionary<string, string>
            {
                { "@ID", input.ID.ToString() },
                { "@IsEnabled", input.IsEnabled ? "1" : "0" },
                { "@FrequencyMinutes", input.FrequencyMinutes.ToString() },
                { "@BackupFolderPath", input.BackupFolderPath },
                { "@RetentionDays", input.RetentionDays.ToString() },
                { "@LastBackupTime", input.LastBackupTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                { "@NextBackupTime", input.NextBackupTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                { "@LastStatus", string.IsNullOrEmpty(input.LastStatus) ? null : input.LastStatus },
                { "@LastErrorMessage", string.IsNullOrEmpty(input.LastErrorMessage) ? null : input.LastErrorMessage },
                { "@LastModifiedBy", string.IsNullOrEmpty(input.LastModifiedBy) ? null : input.LastModifiedBy }
            };
        }
    }
}
