using CoreMedixBeta.Models.Backup;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace CoreMedixBeta.Services
{
    /// <summary>
    /// Result of a backup attempt.
    /// </summary>
    public class BackupRunResult
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; }
        public long HistoryId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long? FileSizeBytes { get; set; }

        public static BackupRunResult Skip(string message)
        {
            return new BackupRunResult { Success = false, Skipped = true, Message = message };
        }

        public static BackupRunResult Fail(string message, long historyId = 0, string filePath = null, long? size = null)
        {
            return new BackupRunResult { Success = false, Skipped = false, Message = message, HistoryId = historyId, FilePath = filePath, FileSizeBytes = size };
        }
    }

    /// <summary>
    /// Result of validating a backup folder path (exists / creatable / writable).
    /// </summary>
    public class FolderCheckResult
    {
        public bool Success { get; set; }
        public bool Exists { get; set; }
        public bool Writable { get; set; }
        public string RequestedPath { get; set; }
        public string ResolvedPath { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Current backup status shown on the settings page.
    /// </summary>
    public class BackupStatusData
    {
        /// <summary>Not Configured | Disabled | Enabled | Running | Error</summary>
        public string StatusText { get; set; } = "Not Configured";
        public bool IsConfigured { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsRunning { get; set; }
        public int IntervalMinutes { get; set; }
        public int RetentionDays { get; set; }
        public DateTime? LastBackupTime { get; set; }
        public DateTime? NextBackupTime { get; set; }
        public bool NextBackupDueNow { get; set; }
        public string LastStatus { get; set; }
        public string LastError { get; set; }
        public string LastBackupFile { get; set; }
        public long? LastBackupSizeBytes { get; set; }
        public string FolderPath { get; set; }
        public bool FolderExists { get; set; }
        public bool FolderWritable { get; set; }
        public string FolderError { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Result of a restore operation.
    /// </summary>
    public class RestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Executes SQL Server BACKUP DATABASE / RESTORE DATABASE operations and
    /// manages dbo.BackupSettings + dbo.BackupHistory.
    ///
    /// This class deliberately uses only System.Data.SqlClient (no Dapper, MVC or
    /// System.Web) so the exact same code can be compiled into the
    /// CoreMedixBeta.BackupRunner console app used by the Windows Scheduled Task.
    ///
    /// Concurrency: every run first takes a SQL Server application lock
    /// (sp_getapplock, session owned) so the web scheduler, the scheduled task and
    /// manual clicks can never run two backups at the same time, across processes.
    /// </summary>
    public static class DatabaseBackupEngine
    {
        private const string LockResource = "CoreMedixBeta.DatabaseBackup";
        private const int MaxHistoryRows = 200;
        private const int MaxRunningMinutes = 120;

        private static readonly object ProbeLock = new object();
        private static FolderCheckResult _cachedProbe;
        private static DateTime _cachedProbeAtUtc = DateTime.MinValue;
        private static int _runningLocally;

        public static bool IsRunningLocally
        {
            get { return Volatile.Read(ref _runningLocally) == 1; }
        }

        #region Connection helpers

        /// <summary>
        /// Resolves the application connection string (single source of truth):
        /// 1) WebConfigPath appSetting - lets the runner read the web app's Web.config.
        /// 2) This application's own config (web app and runner fallback).
        /// 3) Web.config sitting next to the executable (runner deployed into bin).
        /// </summary>
        public static string GetConnectionString()
        {
            string webConfigPath = ConfigurationManager.AppSettings["WebConfigPath"];
            if (!string.IsNullOrWhiteSpace(webConfigPath))
            {
                string cs = ReadConnectionStringFromConfigFile(webConfigPath);
                if (!string.IsNullOrWhiteSpace(cs)) return cs;
            }

            ConnectionStringSettings own = ConfigurationManager.ConnectionStrings["NewConnectionString"];
            if (own != null && !string.IsNullOrWhiteSpace(own.ConnectionString)) return own.ConnectionString;

            string beside = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web.config");
            string csBeside = ReadConnectionStringFromConfigFile(beside);
            if (!string.IsNullOrWhiteSpace(csBeside)) return csBeside;

            throw new InvalidOperationException(
                "Database connection is not configured. Expected connection string 'NewConnectionString' " +
                "(in Web.config, or set 'WebConfigPath' for the backup runner).");
        }

        private static string ReadConnectionStringFromConfigFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                var map = new ExeConfigurationFileMap { ExeConfigFilename = path };
                Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                ConnectionStringSettings cs = cfg.ConnectionStrings.ConnectionStrings["NewConnectionString"];
                return cs == null ? null : cs.ConnectionString;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Actual configured database name (never hardcoded).
        /// </summary>
        public static string GetDatabaseName()
        {
            var csb = new SqlConnectionStringBuilder(GetConnectionString());
            if (string.IsNullOrWhiteSpace(csb.InitialCatalog))
                throw new InvalidOperationException("The connection string does not specify a database name.");
            return csb.InitialCatalog;
        }

        private static SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(GetConnectionString());
            conn.Open();
            return conn;
        }

        #endregion

        #region Settings

        public static string GetDefaultBackupFolder()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Backups");
        }

        private static DatabaseBackupSetting MapSetting(SqlDataReader r)
        {
            return new DatabaseBackupSetting
            {
                ID = r.GetInt32(0),
                IsEnabled = r.GetBoolean(1),
                IntervalMinutes = r.GetInt32(2),
                BackupFolder = r.GetString(3),
                RetentionDays = r.GetInt32(4),
                LastSuccessTime = r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5),
                LastAttemptTime = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
                LastStatus = r.GetString(7),
                LastError = r.IsDBNull(8) ? null : r.GetString(8),
                LastBackupFile = r.IsDBNull(9) ? null : r.GetString(9),
                UpdatedBy = r.IsDBNull(10) ? null : r.GetString(10),
                UpdatedOn = r.IsDBNull(11) ? (DateTime?)null : r.GetDateTime(11)
            };
        }

        private const string SettingsSelectSql =
            @"SELECT TOP 1 ID, IsEnabled, IntervalMinutes, BackupFolder, RetentionDays,
                     LastSuccessTime, LastAttemptTime, LastStatus, LastError, LastBackupFile, UpdatedBy, UpdatedOn
              FROM dbo.BackupSettings";

        public static DatabaseBackupSetting LoadSettings(bool createIfMissing, string defaultFolder = null)
        {
            DatabaseBackupSetting settings = SelectSettings();
            if (settings != null || !createIfMissing) return settings;

            string folder = string.IsNullOrWhiteSpace(defaultFolder) ? GetDefaultBackupFolder() : NormalizeFolder(defaultFolder);
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"INSERT INTO dbo.BackupSettings (IsEnabled, IntervalMinutes, BackupFolder, RetentionDays, LastStatus)
                  VALUES (0, 60, @Folder, 30, 'Never Run');
                  " + SettingsSelectSql + ";", conn))
            {
                cmd.Parameters.AddWithValue("@Folder", folder);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read()) return MapSetting(rdr);
                }
            }
            return SelectSettings();
        }

        private static DatabaseBackupSetting SelectSettings()
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(SettingsSelectSql, conn))
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read()) return MapSetting(rdr);
                return null;
            }
        }

        public static DatabaseBackupSetting SaveSettings(DatabaseBackupSetting input, string user)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (string.IsNullOrWhiteSpace(input.BackupFolder))
                throw new ArgumentException("Backup folder path is required.");
            if (input.IntervalMinutes < 1 || input.IntervalMinutes > 10080)
                throw new ArgumentException("Backup interval must be between 1 and 10080 minutes.");
            if (input.RetentionDays < 0 || input.RetentionDays > 3650)
                throw new ArgumentException("Backup retention must be between 0 and 3650 days.");

            string folder = NormalizeFolder(input.BackupFolder);
            string safeUser = Truncate(user, 100);
            DatabaseBackupSetting existing = SelectSettings();

            using (SqlConnection conn = OpenConnection())
            {
                if (existing != null)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        @"UPDATE dbo.BackupSettings
                          SET IsEnabled = @IsEnabled, IntervalMinutes = @IntervalMinutes,
                              BackupFolder = @BackupFolder, RetentionDays = @RetentionDays,
                              UpdatedBy = @User, UpdatedOn = GETDATE()
                          WHERE ID = @ID", conn))
                    {
                        BindSettings(cmd, input, folder, safeUser);
                        cmd.Parameters.AddWithValue("@ID", existing.ID);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand(
                        @"INSERT INTO dbo.BackupSettings
                              (IsEnabled, IntervalMinutes, BackupFolder, RetentionDays, UpdatedBy, UpdatedOn)
                          VALUES (@IsEnabled, @IntervalMinutes, @BackupFolder, @RetentionDays, @User, GETDATE())", conn))
                    {
                        BindSettings(cmd, input, folder, safeUser);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return SelectSettings();
        }

        private static void BindSettings(SqlCommand cmd, DatabaseBackupSetting input, string folder, string user)
        {
            cmd.Parameters.AddWithValue("@IsEnabled", input.IsEnabled);
            cmd.Parameters.AddWithValue("@IntervalMinutes", input.IntervalMinutes);
            cmd.Parameters.AddWithValue("@BackupFolder", folder);
            cmd.Parameters.AddWithValue("@RetentionDays", input.RetentionDays);
            cmd.Parameters.AddWithValue("@User", (object)user ?? DBNull.Value);
        }

        #endregion

        #region Folder validation

        /// <summary>
        /// Validates that a folder path is valid, exists (or can be created) and is writable.
        /// </summary>
        public static FolderCheckResult ValidateFolder(string path, bool createIfMissing = true)
        {
            var result = new FolderCheckResult { RequestedPath = path };
            if (string.IsNullOrWhiteSpace(path))
            {
                result.Error = "Backup folder path is required.";
                return result;
            }

            string full;
            try
            {
                full = NormalizeFolder(path);
            }
            catch (Exception ex)
            {
                result.Error = "Invalid folder path: " + CleanMessage(ex);
                return result;
            }
            result.ResolvedPath = full;

            try
            {
                if (!Directory.Exists(full))
                {
                    if (!createIfMissing)
                    {
                        result.Error = "Folder does not exist.";
                        return result;
                    }
                    try
                    {
                        Directory.CreateDirectory(full);
                    }
                    catch (Exception ex)
                    {
                        result.Error = "Folder could not be created: " + CleanMessage(ex);
                        return result;
                    }
                }
                result.Exists = true;

                string probePath = Path.Combine(full, ".backup_write_probe_" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllText(probePath, "probe");
                }
                catch (Exception ex)
                {
                    result.Error = "Folder exists but is not writable by the application: " + CleanMessage(ex);
                    return result;
                }
                finally
                {
                    try { if (File.Exists(probePath)) File.Delete(probePath); }
                    catch { }
                }

                result.Writable = true;
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = CleanMessage(ex);
                return result;
            }
        }

        /// <summary>
        /// Throttled folder probe used by status polling (at most one write test per minute).
        /// </summary>
        public static FolderCheckResult CheckFolderCached(string path)
        {
            lock (ProbeLock)
            {
                if (_cachedProbe != null
                    && string.Equals(_cachedProbe.RequestedPath, path, StringComparison.Ordinal)
                    && (DateTime.UtcNow - _cachedProbeAtUtc).TotalSeconds < 60)
                {
                    return _cachedProbe;
                }
                FolderCheckResult probe = ValidateFolder(path, true);
                _cachedProbe = probe;
                _cachedProbeAtUtc = DateTime.UtcNow;
                return probe;
            }
        }

        private static string NormalizeFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Backup folder path is required.");

            string p = path.Trim().Trim('"');
            if (p.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("Backup folder path contains invalid characters.");

            string full = Path.GetFullPath(p);
            if (!Path.IsPathRooted(full))
                throw new ArgumentException("Backup folder must be an absolute path (e.g. D:\\POS_Backups).");

            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        #endregion

        #region Backup execution

        /// <summary>
        /// Runs a backup when one is due according to the saved settings (scheduler entry point).
        /// </summary>
        public static BackupRunResult RunDueBackup(string triggeredBy)
        {
            return RunBackup(triggeredBy, false);
        }

        /// <summary>
        /// Runs a backup immediately regardless of the schedule (Backup Now button).
        /// </summary>
        public static BackupRunResult RunBackupNow(string triggeredBy)
        {
            return RunBackup(triggeredBy, true);
        }

        private static BackupRunResult RunBackup(string triggeredBy, bool force)
        {
            if (Interlocked.CompareExchange(ref _runningLocally, 1, 0) != 0)
                return BackupRunResult.Skip("A backup is already running in this application. Please wait for it to finish.");

            SqlConnection lockConn = null;
            try
            {
                lockConn = OpenConnection();
                if (!TryAcquireLock(lockConn))
                    return BackupRunResult.Skip("A backup or restore is already running (started by another process or user).");

                return RunBackupUnderLock(triggeredBy, force, null, true);
            }
            catch (Exception ex)
            {
                return BackupRunResult.Fail(CleanMessage(ex));
            }
            finally
            {
                if (lockConn != null)
                {
                    try { lockConn.Dispose(); }
                    catch { }
                }
                Interlocked.Exchange(ref _runningLocally, 0);
            }
        }

        /// <summary>
        /// Performs the actual backup while the caller already holds the application lock.
        /// </summary>
        /// <param name="triggeredBy">User or service name recorded in history/audit.</param>
        /// <param name="force">True = run even if not due / disabled (manual or pre-restore safety backup).</param>
        /// <param name="backupTypeOverride">Scheduled | Manual | PreRestore; null = derive from force.</param>
        /// <param name="applyRetention">Run retention cleanup after a successful backup.</param>
        private static BackupRunResult RunBackupUnderLock(string triggeredBy, bool force, string backupTypeOverride, bool applyRetention)
        {
            string user = string.IsNullOrWhiteSpace(triggeredBy) ? "System" : triggeredBy;
            string safeUser = Truncate(user, 100);

            DatabaseBackupSetting settings = LoadSettings(false);
            if (settings == null)
                return BackupRunResult.Skip("Backup settings have not been configured yet. Open Settings \u2192 Database Backup to configure automatic backups.");

            if (!force)
            {
                if (!settings.IsEnabled)
                    return BackupRunResult.Skip("Automatic backup is currently disabled.");
                if (!IsDue(settings))
                    return BackupRunResult.Skip("Backup is not due yet.");
            }

            string dbName = GetDatabaseName();
            string backupType = backupTypeOverride ?? (force ? "Manual" : "Scheduled");

            // --- folder must be valid before SQL Server is asked to write anything ---
            FolderCheckResult folder = ValidateFolder(settings.BackupFolder, true);
            if (!folder.Success)
            {
                string folderError = "Backup folder is not usable: " + folder.Error;
                string failFileName = SanitizeFileName(dbName) + "_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
                string failFilePath = Path.Combine(settings.BackupFolder, failFileName);
                long folderFailId = InsertHistoryRow(dbName, failFileName, failFilePath,
                    backupType, "Failed", folderError, safeUser);
                UpdateSettingsAfterRun(settings, false, folderError, null);
                LogActivity(safeUser, "BACKUP_FAILED", "DatabaseBackup", folderFailId.ToString(), folderError, null);
                return BackupRunResult.Fail(folderError, folderFailId, failFilePath, null);
            }

            string fileName = BuildFileName(dbName, folder.ResolvedPath);
            string filePath = Path.Combine(folder.ResolvedPath, fileName);
            long historyId = InsertHistoryRow(dbName, fileName, filePath, backupType, "Running", null, safeUser);

            // --- run BACKUP DATABASE ---
            string error = null;
            try
            {
                ExecuteBackup(dbName, filePath);
            }
            catch (Exception ex)
            {
                error = CleanMessage(ex);
            }

            // --- SQL Server must confirm the file can be read back ---
            if (error == null)
            {
                try
                {
                    ExecuteVerifyOnly(filePath);
                }
                catch (Exception ex)
                {
                    error = "Backup file verification failed: " + CleanMessage(ex);
                }
            }

            long? size = null;
            try
            {
                if (File.Exists(filePath)) size = new FileInfo(filePath).Length;
            }
            catch { }

            if (error != null)
            {
                UpdateHistoryRow(historyId, "Failed", size, error);
                UpdateSettingsAfterRun(settings, false, error, null);
                LogActivity(safeUser, "BACKUP_FAILED", "DatabaseBackup", historyId.ToString(), error, null);
                return BackupRunResult.Fail(error, historyId, filePath, size);
            }

            UpdateHistoryRow(historyId, "Success", size, null);
            UpdateSettingsAfterRun(settings, true, null, fileName);
            LogActivity(safeUser, "BACKUP", "DatabaseBackup", historyId.ToString(),
                "Backup '" + fileName + "' completed successfully.", null);

            string cleanupWarning = null;
            if (applyRetention)
            {
                cleanupWarning = ApplyRetention(settings, safeUser);
            }

            string message = "Database backup completed successfully: " + fileName;
            if (cleanupWarning != null)
            {
                message += " | Cleanup warning: " + cleanupWarning;
            }

            return new BackupRunResult
            {
                Success = true,
                HistoryId = historyId,
                FileName = fileName,
                FilePath = filePath,
                FileSizeBytes = size,
                Message = message
            };
        }

        /// <summary>
        /// True when the interval has elapsed since the last attempt (computed in SQL to
        /// avoid any web/runner clock differences).
        /// </summary>
        private static bool IsDue(DatabaseBackupSetting settings)
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT CASE WHEN IsEnabled = 1
                               AND (LastAttemptTime IS NULL
                                    OR DATEADD(MINUTE, IntervalMinutes, LastAttemptTime) <= GETDATE())
                              THEN 1 ELSE 0 END
                  FROM dbo.BackupSettings WHERE ID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", settings.ID);
                object o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return false;
                return Convert.ToInt32(o) == 1;
            }
        }

        private static void ExecuteBackup(string dbName, string filePath)
        {
            string dbLiteral = BracketLiteral(dbName);
            string withCompression = "BACKUP DATABASE " + dbLiteral + " TO DISK = @Path WITH INIT, COMPRESSION, STATS = 10";
            string withoutCompression = "BACKUP DATABASE " + dbLiteral + " TO DISK = @Path WITH INIT, STATS = 10";

            try
            {
                ExecuteScalarSql(withCompression, filePath, 0);
            }
            catch (SqlException ex)
            {
                // Editions without backup compression: retry once without COMPRESSION.
                string msg = ex.Message ?? string.Empty;
                bool unsupported = msg.IndexOf("compression", StringComparison.OrdinalIgnoreCase) >= 0
                    && (msg.IndexOf("not supported", StringComparison.OrdinalIgnoreCase) >= 0
                        || msg.IndexOf("edition", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!unsupported) throw;
                ExecuteScalarSql(withoutCompression, filePath, 0);
            }
        }

        private static void ExecuteVerifyOnly(string filePath)
        {
            ExecuteScalarSql("RESTORE VERIFYONLY FROM DISK = @Path WITH STATS = 10", filePath, 0);
        }

        /// <summary>
        /// Executes a command with a single @Path disk parameter. commandTimeoutSeconds 0 = no timeout.
        /// </summary>
        private static void ExecuteScalarSql(string sql, string path, int commandTimeoutSeconds)
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 600).Value = path;
                cmd.CommandTimeout = commandTimeoutSeconds;
                cmd.ExecuteNonQuery();
            }
        }

        private static string BuildFileName(string dbName, string folder)
        {
            string safeDb = SanitizeFileName(dbName);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string name = safeDb + "_Backup_" + stamp + ".bak";
            int suffix = 1;
            while (File.Exists(Path.Combine(folder, name)))
            {
                name = safeDb + "_Backup_" + stamp + "_" + suffix + ".bak";
                suffix++;
            }
            return name;
        }

        private static long InsertHistoryRow(string dbName, string fileName, string filePath,
            string backupType, string status, string errorMessage, string user, long? fileSize = null)
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"INSERT INTO dbo.BackupHistory
                      (FileName, FilePath, DatabaseName, BackupType, Status, FileSizeBytes,
                       StartedOn, CompletedOn, ErrorMessage, CreatedBy)
                  VALUES (@FileName, @FilePath, @DatabaseName, @BackupType, @Status, @FileSize,
                          GETDATE(), CASE WHEN @Status = 'Running' THEN NULL ELSE GETDATE() END, @Error, @User);
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);", conn))
            {
                cmd.Parameters.AddWithValue("@FileName", fileName);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.Parameters.AddWithValue("@DatabaseName", dbName);
                cmd.Parameters.AddWithValue("@BackupType", backupType);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@FileSize", (object)fileSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Error", (object)Truncate(errorMessage, 1000) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@User", Truncate(user, 100));
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private static void UpdateHistoryRow(long historyId, string status, long? fileSize, string errorMessage)
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"UPDATE dbo.BackupHistory
                  SET Status = @Status,
                      FileSizeBytes = COALESCE(@FileSize, FileSizeBytes),
                      CompletedOn = GETDATE(),
                      ErrorMessage = @Error
                  WHERE ID = @ID AND Status = 'Running'", conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@FileSize", (object)fileSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Error", (object)Truncate(errorMessage, 1000) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID", historyId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateSettingsAfterRun(DatabaseBackupSetting settings, bool success, string errorMessage, string fileName)
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"UPDATE dbo.BackupSettings
                  SET LastAttemptTime = GETDATE(),
                      LastSuccessTime = CASE WHEN @Ok = 1 THEN GETDATE() ELSE LastSuccessTime END,
                      LastStatus = @Status,
                      LastError = @Error,
                      LastBackupFile = CASE WHEN @Ok = 1 THEN @File ELSE LastBackupFile END
                  WHERE ID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@Ok", success);
                cmd.Parameters.AddWithValue("@Status", success ? "Success" : "Failed");
                cmd.Parameters.AddWithValue("@Error", success ? DBNull.Value : (object)Truncate(errorMessage, 1000));
                cmd.Parameters.AddWithValue("@File", (object)Truncate(fileName, 300) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID", settings.ID);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Application lock (prevents overlapping jobs across processes)

        private static bool TryAcquireLock(SqlConnection conn)
        {
            using (SqlCommand cmd = new SqlCommand("sp_getapplock", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Resource", LockResource);
                cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
                cmd.Parameters.AddWithValue("@LockOwner", "Session");
                cmd.Parameters.AddWithValue("@LockTimeout", 0);
                SqlParameter ret = cmd.Parameters.Add("@ReturnValue", SqlDbType.Int);
                ret.Direction = ParameterDirection.ReturnValue;
                cmd.ExecuteNonQuery();
                int rc = ret.Value == null || ret.Value == DBNull.Value ? -1 : (int)ret.Value;
                return rc >= 0; // 0 = granted, 1 = granted after waiting
            }
        }

        #endregion

        #region Status & history

        /// <summary>
        /// Marks runs that were interrupted (recycled IIS, killed task) as failed so the UI
        /// never shows a permanently "Running" state.
        /// </summary>
        private static void HealStaleRuns()
        {
            try
            {
                using (SqlConnection conn = OpenConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.BackupHistory
                      SET Status = 'Failed',
                          CompletedOn = GETDATE(),
                          ErrorMessage = 'Backup was interrupted (process stopped or application recycled).'
                      WHERE Status = 'Running' AND StartedOn <= DATEADD(MINUTE, -@Minutes, GETDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@Minutes", MaxRunningMinutes);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public static BackupStatusData GetStatus()
        {
            var data = new BackupStatusData();
            try
            {
                HealStaleRuns();

                DatabaseBackupSetting settings = LoadSettings(false);
                if (settings == null)
                {
                    data.StatusText = "Not Configured";
                    data.FolderPath = GetDefaultBackupFolder();
                    FolderCheckResult def = CheckFolderCached(data.FolderPath);
                    data.FolderExists = def.Exists;
                    data.FolderWritable = def.Writable;
                    data.FolderError = def.Error;
                    return data;
                }

                data.IsConfigured = true;
                data.IsEnabled = settings.IsEnabled;
                data.IntervalMinutes = settings.IntervalMinutes;
                data.RetentionDays = settings.RetentionDays;
                data.LastBackupTime = settings.LastSuccessTime;
                data.LastStatus = settings.LastStatus;
                data.LastError = settings.LastError;
                data.LastBackupFile = settings.LastBackupFile;

                data.FolderPath = settings.BackupFolder;
                FolderCheckResult folder = CheckFolderCached(settings.BackupFolder);
                data.FolderExists = folder.Exists;
                data.FolderWritable = folder.Writable;
                data.FolderError = folder.Error;

                int runningCount = 0;
                using (SqlConnection conn = OpenConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM dbo.BackupHistory
                      WHERE Status = 'Running' AND StartedOn > DATEADD(MINUTE, -@Minutes, GETDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@Minutes", MaxRunningMinutes);
                    runningCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
                data.IsRunning = IsRunningLocally || runningCount > 0;

                // Latest known-good backup (name + size) even if settings were edited since
                using (SqlConnection conn = OpenConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT TOP 1 FileName, FileSizeBytes FROM dbo.BackupHistory
                      WHERE Status = 'Success' AND IsDeleted = 0
                      ORDER BY StartedOn DESC, ID DESC", conn))
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            data.LastBackupFile = rdr.IsDBNull(0) ? data.LastBackupFile : rdr.GetString(0);
                            data.LastBackupSizeBytes = rdr.IsDBNull(1) ? (long?)null : rdr.GetInt64(1);
                        }
                    }
                }

                if (data.IsRunning)
                {
                    data.StatusText = "Running";
                }
                else if (string.Equals(settings.LastStatus, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    data.StatusText = "Error";
                }
                else
                {
                    data.StatusText = settings.IsEnabled ? "Enabled" : "Disabled";
                }

                if (settings.IsEnabled)
                {
                    if (settings.LastSuccessTime.HasValue)
                        data.NextBackupTime = settings.LastSuccessTime.Value.AddMinutes(settings.IntervalMinutes);
                    else if (settings.LastAttemptTime.HasValue)
                        data.NextBackupTime = settings.LastAttemptTime.Value.AddMinutes(settings.IntervalMinutes);
                    else
                        data.NextBackupDueNow = true;
                }
            }
            catch (Exception ex)
            {
                data.StatusText = "Error";
                data.Error = CleanMessage(ex);
            }
            return data;
        }

        public static List<BackupHistoryEntry> GetHistory(int maxRows)
        {
            if (maxRows < 1) maxRows = 100;
            if (maxRows > MaxHistoryRows) maxRows = MaxHistoryRows;
            HealStaleRuns();

            var list = new List<BackupHistoryEntry>();
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT TOP (@Max) ID, FileName, FilePath, DatabaseName, BackupType, Status, FileSizeBytes,
                         StartedOn, CompletedOn, ErrorMessage, CreatedBy, IsDeleted, DeletedOn, DeletedReason,
                         RestoredOn, RestoredBy
                  FROM dbo.BackupHistory
                  ORDER BY StartedOn DESC, ID DESC", conn))
            {
                cmd.Parameters.AddWithValue("@Max", maxRows);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) list.Add(MapHistory(rdr));
                }
            }
            return list;
        }

        public static BackupHistoryEntry GetHistoryRow(long id)
        {
            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT ID, FileName, FilePath, DatabaseName, BackupType, Status, FileSizeBytes,
                         StartedOn, CompletedOn, ErrorMessage, CreatedBy, IsDeleted, DeletedOn, DeletedReason,
                         RestoredOn, RestoredBy
                  FROM dbo.BackupHistory WHERE ID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read()) return MapHistory(rdr);
                    return null;
                }
            }
        }

        private static BackupHistoryEntry MapHistory(SqlDataReader r)
        {
            return new BackupHistoryEntry
            {
                ID = r.GetInt64(0),
                FileName = r.GetString(1),
                FilePath = r.GetString(2),
                DatabaseName = r.GetString(3),
                BackupType = r.GetString(4),
                Status = r.GetString(5),
                FileSizeBytes = r.IsDBNull(6) ? (long?)null : r.GetInt64(6),
                StartedOn = r.GetDateTime(7),
                CompletedOn = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8),
                ErrorMessage = r.IsDBNull(9) ? null : r.GetString(9),
                CreatedBy = r.GetString(10),
                IsDeleted = !r.IsDBNull(11) && r.GetBoolean(11),
                DeletedOn = r.IsDBNull(12) ? (DateTime?)null : r.GetDateTime(12),
                DeletedReason = r.IsDBNull(13) ? null : r.GetString(13),
                RestoredOn = r.IsDBNull(14) ? (DateTime?)null : r.GetDateTime(14),
                RestoredBy = r.IsDBNull(15) ? null : r.GetString(15)
            };
        }

        #endregion

        #region Download / delete / file resolution

        /// <summary>
        /// Resolves the physical file for a history row with strict safety checks.
        /// The path always originates from the database (never from user input) and is
        /// re-validated against the stored file name and .bak extension.
        /// </summary>
        private static bool TryResolveHistoryFile(BackupHistoryEntry row, out string fullPath, out string error)
        {
            fullPath = null;
            error = null;
            if (row == null)
            {
                error = "Backup record not found.";
                return false;
            }
            if (row.IsDeleted)
            {
                error = "This backup file has been removed.";
                return false;
            }
            try
            {
                string p = Path.GetFullPath(row.FilePath.Trim().Trim('"'));
                if (!Path.IsPathRooted(p))
                {
                    error = "Invalid backup file location.";
                    return false;
                }
                if (!string.Equals(Path.GetFileName(p), row.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    error = "Backup record mismatch - operation refused.";
                    return false;
                }
                if (!p.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Only .bak backup files can be accessed.";
                    return false;
                }
                fullPath = p;
                return true;
            }
            catch (Exception ex)
            {
                error = "Invalid backup file location: " + CleanMessage(ex);
                return false;
            }
        }

        public static bool TryGetDownloadPath(long id, out string fullPath, out string fileName, out string error)
        {
            fullPath = null;
            fileName = null;
            error = null;

            BackupHistoryEntry row = GetHistoryRow(id);
            if (row == null)
            {
                error = "Backup record not found.";
                return false;
            }
            if (!string.Equals(row.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                error = "Only verified successful backups can be downloaded.";
                return false;
            }
            if (!TryResolveHistoryFile(row, out fullPath, out error)) return false;

            if (!File.Exists(fullPath))
            {
                error = "The backup file no longer exists on the server (it may have been moved or removed).";
                return false;
            }
            fileName = row.FileName;
            return true;
        }

        public static bool DeleteBackup(long id, string user, out string error)
        {
            error = null;
            BackupHistoryEntry row = GetHistoryRow(id);
            if (row == null)
            {
                error = "Backup record not found.";
                return false;
            }
            if (string.Equals(row.Status, "Running", StringComparison.OrdinalIgnoreCase))
            {
                error = "This backup is currently running and cannot be deleted.";
                return false;
            }
            if (row.IsDeleted)
            {
                error = "This backup has already been removed.";
                return false;
            }

            string path;
            if (!TryResolveHistoryFile(row, out path, out error)) return false;

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                error = "Could not delete the backup file: " + CleanMessage(ex);
                LogActivity(user, "BACKUP_DELETE_FAILED", "DatabaseBackup", id.ToString(), error, null);
                return false;
            }

            using (SqlConnection conn = OpenConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"UPDATE dbo.BackupHistory
                  SET IsDeleted = 1, DeletedOn = GETDATE(), DeletedReason = 'Manual'
                  WHERE ID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.ExecuteNonQuery();
            }

            LogActivity(user, "BACKUP_DELETE", "DatabaseBackup", id.ToString(),
                "Backup file '" + row.FileName + "' deleted manually.", null);
            return true;
        }

        #endregion

        #region Retention cleanup

        /// <summary>
        /// Deletes managed backup files older than the retention window.
        /// Never deletes: files not created by this feature, running backups, or the
        /// most recent known-good backup. Returns a warning when anything could not be removed.
        /// </summary>
        private static string ApplyRetention(DatabaseBackupSetting settings, string user)
        {
            if (settings.RetentionDays <= 0) return null;

            var failures = new List<string>();
            int removed = 0;

            try
            {
                List<BackupHistoryEntry> candidates;
                using (SqlConnection conn = OpenConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT TOP 50 ID, FileName, FilePath, DatabaseName, BackupType, Status, FileSizeBytes,
                             StartedOn, CompletedOn, ErrorMessage, CreatedBy, IsDeleted, DeletedOn, DeletedReason,
                             RestoredOn, RestoredBy
                      FROM dbo.BackupHistory
                      WHERE Status = 'Success'
                        AND IsDeleted = 0
                        AND StartedOn < DATEADD(DAY, -@Days, GETDATE())
                        AND ID <> (SELECT TOP 1 ID FROM dbo.BackupHistory
                                   WHERE Status = 'Success' AND IsDeleted = 0
                                   ORDER BY StartedOn DESC, ID DESC)
                      ORDER BY StartedOn DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@Days", settings.RetentionDays);
                    candidates = new List<BackupHistoryEntry>();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) candidates.Add(MapHistory(rdr));
                    }
                }

                foreach (BackupHistoryEntry row in candidates)
                {
                    string path;
                    string resolveError;
                    if (!TryResolveHistoryFile(row, out path, out resolveError))
                    {
                        failures.Add(row.FileName + ": " + resolveError);
                        LogActivity(user, "BACKUP_CLEANUP_FAILED", "DatabaseBackup", row.ID.ToString(), resolveError, null);
                        continue;
                    }

                    try
                    {
                        if (File.Exists(path)) File.Delete(path);

                        using (SqlConnection conn = OpenConnection())
                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE dbo.BackupHistory
                              SET IsDeleted = 1, DeletedOn = GETDATE(), DeletedReason = 'Retention'
                              WHERE ID = @ID", conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", row.ID);
                            cmd.ExecuteNonQuery();
                        }
                        removed++;
                        LogActivity(user, "BACKUP_CLEANUP", "DatabaseBackup", row.ID.ToString(),
                            "Backup '" + row.FileName + "' removed by retention policy (" + settings.RetentionDays + " days).", null);
                    }
                    catch (Exception ex)
                    {
                        string msg = CleanMessage(ex);
                        failures.Add(row.FileName + ": " + msg);
                        LogActivity(user, "BACKUP_CLEANUP_FAILED", "DatabaseBackup", row.ID.ToString(), msg, null);
                    }
                }

                if (failures.Count > 0)
                {
                    return removed + " old backup(s) removed, but " + failures.Count
                        + " could not be deleted (backups were preserved): " + string.Join("; ", failures);
                }
                if (removed > 0)
                {
                    LogActivity(user, "BACKUP_CLEANUP_SUMMARY", "DatabaseBackup", "-",
                        removed + " backup file(s) removed by retention policy.", null);
                }
                return null;
            }
            catch (Exception ex)
            {
                return "Retention cleanup failed (backups were preserved): " + CleanMessage(ex);
            }
        }

        #endregion

        #region Restore

        public static RestoreResult RestoreBackup(long id, string confirmText, string user)
        {
            string safeUser = Truncate(user, 100);

            if (!string.Equals((confirmText ?? string.Empty).Trim(), "RESTORE", StringComparison.OrdinalIgnoreCase))
                return new RestoreResult { Success = false, Message = "Confirmation text must be exactly RESTORE." };

            BackupHistoryEntry row = GetHistoryRow(id);
            if (row == null)
                return new RestoreResult { Success = false, Message = "Backup record not found." };
            if (row.IsDeleted || !string.Equals(row.Status, "Success", StringComparison.OrdinalIgnoreCase))
                return new RestoreResult { Success = false, Message = "Only verified successful backups can be restored." };

            string path;
            string resolveError;
            if (!TryResolveHistoryFile(row, out path, out resolveError))
                return new RestoreResult { Success = false, Message = resolveError };
            if (!File.Exists(path))
                return new RestoreResult { Success = false, Message = "The backup file no longer exists on the server." };

            if (Interlocked.CompareExchange(ref _runningLocally, 1, 0) != 0)
                return new RestoreResult { Success = false, Message = "A backup or restore is already running in this application." };

            SqlConnection lockConn = null;
            try
            {
                lockConn = OpenConnection();
                if (!TryAcquireLock(lockConn))
                    return new RestoreResult { Success = false, Message = "Another backup or restore is currently in progress. Please try again shortly." };

                LogActivity(safeUser, "BACKUP_RESTORE_ATTEMPT", "DatabaseBackup", id.ToString(),
                    "Restore of backup '" + row.FileName + "' started.", null);

                // 1) Safety backup of the CURRENT database before overwriting anything
                BackupRunResult safety = RunBackupUnderLock(safeUser, true, "PreRestore", false);
                if (!safety.Success)
                {
                    string abort = "Restore aborted because the safety backup of the current database failed. "
                        + safety.Message;
                    LogActivity(safeUser, "BACKUP_RESTORE_FAILED", "DatabaseBackup", id.ToString(), abort, null);
                    return new RestoreResult { Success = false, Message = abort };
                }

                // 2) Verify the selected backup can be read by SQL Server
                try
                {
                    ExecuteVerifyOnly(path);
                }
                catch (Exception ex)
                {
                    string abort = "Restore aborted because the selected backup file failed verification: " + CleanMessage(ex);
                    LogActivity(safeUser, "BACKUP_RESTORE_FAILED", "DatabaseBackup", id.ToString(), abort, null);
                    return new RestoreResult { Success = false, Message = abort };
                }

                // 3) Restore (drops active connections to the POS database - see UI warning)
                string dbName = GetDatabaseName();
                ExecuteRestore(dbName, path);
                SqlConnection.ClearAllPools();

                using (SqlConnection conn = OpenConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.BackupHistory SET RestoredOn = GETDATE(), RestoredBy = @User WHERE ID = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@User", safeUser);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }

                LogActivity(safeUser, "BACKUP_RESTORE", "DatabaseBackup", id.ToString(),
                    "Backup '" + row.FileName + "' restored successfully. A safety backup was taken first as '" + safety.FileName + "'.", null);

                return new RestoreResult
                {
                    Success = true,
                    Message = "Database restored successfully from '" + row.FileName
                        + "'. Safety backup of the previous data: '" + safety.FileName + "'."
                };
            }
            catch (Exception ex)
            {
                string msg = "Restore failed: " + CleanMessage(ex);
                LogActivity(safeUser, "BACKUP_RESTORE_FAILED", "DatabaseBackup", id.ToString(), msg, null);
                return new RestoreResult { Success = false, Message = msg };
            }
            finally
            {
                if (lockConn != null)
                {
                    try { lockConn.Dispose(); }
                    catch { }
                }
                Interlocked.Exchange(ref _runningLocally, 0);
            }
        }

        private static void ExecuteRestore(string dbName, string filePath)
        {
            string dbLiteral = BracketLiteral(dbName);
            string masterCs = new SqlConnectionStringBuilder(GetConnectionString())
            {
                InitialCatalog = "master",
                ConnectTimeout = 15
            }.ConnectionString;

            using (var conn = new SqlConnection(masterCs))
            {
                conn.Open();

                RunNonQuery(conn, "ALTER DATABASE " + dbLiteral + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", null);

                bool restoreSucceeded = false;
                try
                {
                    RunNonQuery(conn,
                        "RESTORE DATABASE " + dbLiteral + " FROM DISK = @Path WITH REPLACE, RECOVERY, STATS = 10;",
                        filePath);
                    restoreSucceeded = true;
                }
                finally
                {
                    if (!restoreSucceeded)
                    {
                        // The restore failed after the database went offline - bring it back if possible.
                        try { RunNonQuery(conn, "RESTORE DATABASE " + dbLiteral + " WITH RECOVERY;", null); }
                        catch { }
                    }

                    // Always hand the database back to normal multi-user operation.
                    try
                    {
                        using (var stateCmd = new SqlCommand(
                            "SELECT user_access_desc FROM sys.databases WHERE name = @Name", conn))
                        {
                            stateCmd.Parameters.AddWithValue("@Name", dbName);
                            string state = stateCmd.ExecuteScalar() as string;
                            if (string.Equals(state, "SINGLE_USER", StringComparison.OrdinalIgnoreCase))
                            {
                                RunNonQuery(conn, "ALTER DATABASE " + dbLiteral + " SET MULTI_USER;", null);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private static void RunNonQuery(SqlConnection conn, string sql, string path)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                if (path != null)
                    cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 600).Value = path;
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Audit & helpers

        /// <summary>
        /// Writes to the existing dbo.ActivityLogs audit table. Audit logging must never
        /// break a backup or restore operation.
        /// </summary>
        public static void LogActivity(string username, string action, string entityType, string entityId, string details, string ipAddress)
        {
            try
            {
                using (SqlConnection conn = OpenConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO dbo.ActivityLogs (Username, Action, EntityType, EntityID, Details, IPAddress, CreatedOn)
                      VALUES (@Username, @Action, @EntityType, @EntityID, @Details, @IPAddress, GETDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", (object)Truncate(username, 100) ?? "System");
                    cmd.Parameters.AddWithValue("@Action", (object)Truncate(action, 50) ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@EntityType", (object)Truncate(entityType, 100) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EntityID", (object)Truncate(entityId, 100) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Details", (object)Truncate(details, 1000) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IPAddress", (object)Truncate(ipAddress, 50) ?? "Local");
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static string BracketLiteral(string value)
        {
            return "[" + value.Replace("]", "]]") + "]";
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? "Database" : s;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Returns a safe, single-line error message (no stack traces, no credentials).
        /// </summary>
        private static string CleanMessage(Exception ex)
        {
            if (ex == null) return "Unknown error.";
            string msg = ex.Message ?? ex.GetType().Name;
            msg = msg.Replace("\r", " ").Replace("\n", " ").Trim();
            if (msg.Length > 900) msg = msg.Substring(0, 900);
            return msg;
        }

        #endregion
    }
}
