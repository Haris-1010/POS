using CoreMedixBeta.Models.BackupSettings;
using CoreMedixBeta.Models.BackupSettings.DTOs;
using CoreMedixBeta.Repositories;
using Dapper;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMedixBeta.Service
{
    /// <summary>
    /// Executes SQL Server native BACKUP DATABASE / RESTORE DATABASE operations.
    /// All connections are opened against the "master" database using the same
    /// server and security settings as the application connection string, so no
    /// credentials or server names are ever hard coded.
    /// </summary>
    public static class BackupService
    {
        private const string AppLockResource = "CoreMedixBeta.DatabaseBackup";
        private const int BackupCommandTimeoutSeconds = 3600;

        private static readonly SemaphoreSlim OperationLock = new SemaphoreSlim(1, 1);
        private static int _isOperationRunning;

        public static bool IsBackupRunning => Volatile.Read(ref _isOperationRunning) == 1;

        private static string AppConnectionString =>
            System.Configuration.ConfigurationManager.ConnectionStrings["NewConnectionString"].ConnectionString;

        public static string GetDatabaseName()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(AppConnectionString);
                return builder.InitialCatalog;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildMasterConnectionString()
        {
            var builder = new SqlConnectionStringBuilder(AppConnectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ConnectionString;
        }

        #region Folder helpers

        public static (bool Success, string Message, bool Created) ValidateFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return (false, "Backup folder path is required.", false);

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(folderPath.Trim());
            }
            catch (Exception)
            {
                return (false, "The backup folder path is not a valid Windows path.", false);
            }

            bool created = false;
            try
            {
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    created = true;
                }
            }
            catch (Exception ex)
            {
                return (false, "The backup folder could not be created: " + SanitizeErrorMessage(ex.Message, fullPath), false);
            }

            string testFile = Path.Combine(fullPath, ".pos_write_test_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(testFile, "write-test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                return (false, "The backup folder is not writable by the application: " + SanitizeErrorMessage(ex.Message, fullPath), created);
            }

            return (true, created ? "Folder created and verified writable." : "Folder verified accessible and writable.", created);
        }

        public static string DescribeFolderStatus(string folderPath, out bool exists, out bool writable, out string freeSpace)
        {
            exists = false;
            writable = false;
            freeSpace = "-";

            if (string.IsNullOrWhiteSpace(folderPath))
                return "No backup folder configured.";

            try
            {
                string fullPath = Path.GetFullPath(folderPath.Trim());
                exists = Directory.Exists(fullPath);

                string testFile = Path.Combine(fullPath, ".pos_write_test_" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    if (!exists) Directory.CreateDirectory(fullPath);
                    File.WriteAllText(testFile, "write-test");
                    File.Delete(testFile);
                    writable = true;
                }
                catch
                {
                    writable = false;
                }

                try
                {
                    string root = Path.GetPathRoot(fullPath);
                    var drive = new DriveInfo(root);
                    freeSpace = FormatFileSize(drive.AvailableFreeSpace);
                }
                catch
                {
                    freeSpace = "-";
                }

                if (!exists) return "Folder does not exist and could not be created.";
                if (!writable) return "Folder exists but is not writable by the application.";
                return "Folder is accessible and writable.";
            }
            catch (Exception)
            {
                return "The backup folder path is invalid.";
            }
        }

        public static bool IsPathInsideFolder(string folder, string candidate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(candidate))
                    return false;

                string fullFolder = Path.GetFullPath(folder.Trim())
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullCandidate = Path.GetFullPath(candidate.Trim());

                return fullCandidate.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string FormatFileSize(long? bytes)
        {
            if (!bytes.HasValue) return "-";
            double size = bytes.Value;
            if (size >= 1073741824) return (size / 1073741824.0).ToString("0.00") + " GB";
            if (size >= 1048576) return (size / 1048576.0).ToString("0.00") + " MB";
            if (size >= 1024) return (size / 1024.0).ToString("0.00") + " KB";
            return size.ToString("0") + " B";
        }

        public static DateTime? ComputeNextBackupTime(DateTime? lastBackupTime, int frequencyMinutes, bool isEnabled)
        {
            if (!isEnabled) return null;
            if (frequencyMinutes < 1440) frequencyMinutes = 1440;
            if (lastBackupTime.HasValue) return lastBackupTime.Value.AddMinutes(frequencyMinutes);
            return DateTime.Now.AddMinutes(frequencyMinutes);
        }

        #endregion Folder helpers

        #region Error sanitizing

        /// <summary>
        /// Removes server names, database names, backup folder paths and anything
        /// that looks like credentials from error text before it is stored or shown.
        /// </summary>
        public static string SanitizeErrorMessage(string message, string folderPath = null)
        {
            if (string.IsNullOrEmpty(message)) return null;

            try
            {
                var builder = new SqlConnectionStringBuilder(AppConnectionString);

                if (!string.IsNullOrWhiteSpace(builder.DataSource))
                    message = Regex.Replace(message, Regex.Escape(builder.DataSource), "[server]", RegexOptions.IgnoreCase);

                if (!string.IsNullOrWhiteSpace(builder.InitialCatalog))
                    message = Regex.Replace(message,
                        @"(?<![A-Za-z0-9_])" + Regex.Escape(builder.InitialCatalog) + @"(?![A-Za-z0-9_])",
                        "[database]", RegexOptions.IgnoreCase);
            }
            catch
            {
                // Connection string could not be parsed; continue with remaining rules.
            }

            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                try
                {
                    string fullFolder = Path.GetFullPath(folderPath.Trim())
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    message = Regex.Replace(message, Regex.Escape(fullFolder), "[backup-folder]", RegexOptions.IgnoreCase);
                    message = Regex.Replace(message, Regex.Escape(folderPath.Trim()), "[backup-folder]", RegexOptions.IgnoreCase);
                }
                catch
                {
                    // Invalid folder path; skip this rule.
                }
            }

            message = Regex.Replace(message, @"(?i)(Password|Pwd|User\s*ID|UID)\s*=\s*[^;]+", "$1=***");

            if (message.Length > 2000)
                message = message.Substring(0, 2000) + " ...";

            return message;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        #endregion Error sanitizing

        #region Backup

        public static async Task<BackupOperationResultDto> RunBackupAsync(string triggerType, string performedBy)
        {
            if (!await OperationLock.WaitAsync(0))
            {
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "Another backup or restore operation is already in progress. Please try again shortly."
                };
            }

            Volatile.Write(ref _isOperationRunning, 1);
            try
            {
                return await RunBackupCoreAsync(triggerType, performedBy);
            }
            finally
            {
                Volatile.Write(ref _isOperationRunning, 0);
                OperationLock.Release();
            }
        }

        private static async Task<BackupOperationResultDto> RunBackupCoreAsync(string triggerType, string performedBy)
        {
            var repository = new BackupSettingsRepository();

            BackupSetting settings = await repository.GetSettingsAsync();
            if (settings == null || settings.ID == 0)
            {
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "Backup settings could not be loaded. Open Database Backup Settings and save the configuration first."
                };
            }

            string folder = string.IsNullOrWhiteSpace(settings.BackupFolderPath)
                ? BackupSettingsRepository.GetDefaultBackupFolder()
                : settings.BackupFolderPath.Trim();

            var folderCheck = ValidateFolder(folder);
            if (!folderCheck.Success)
            {
                await repository.UpdateScheduleStateAsync(settings.ID, settings.LastBackupTime,
                    ComputeNextBackupTime(settings.LastBackupTime, settings.FrequencyMinutes, settings.IsEnabled),
                    "Error", folderCheck.Message);
                await repository.InsertAuditLogAsync("Backup", "Failed", "Folder validation failed: " + folderCheck.Message, performedBy);
                return new BackupOperationResultDto { Success = false, Message = folderCheck.Message };
            }

            string dbName = GetDatabaseName();
            if (string.IsNullOrWhiteSpace(dbName))
            {
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "The database name could not be determined from the application configuration."
                };
            }

            string fileName = $"{SanitizeFileName(dbName)}_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string fullPath = Path.Combine(folder, fileName);

            int historyId = await repository.InsertHistoryEntryAsync(new BackupHistoryEntry
            {
                BackupFileName = fileName,
                BackupFilePath = fullPath,
                BackupStartTime = DateTime.Now,
                Status = "Running",
                TriggerType = triggerType,
                CreatedBy = performedBy
            });

            try
            {
                using (var connection = new SqlConnection(BuildMasterConnectionString()))
                {
                    await connection.OpenAsync();

                    int lockResult = await connection.ExecuteScalarAsync<int>(
                        @"DECLARE @res INT;
                          EXEC @res = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0;
                          SELECT @res;",
                        new { Resource = AppLockResource });

                    if (lockResult < 0)
                    {
                        throw new InvalidOperationException("Another backup or restore operation is already running on the server.");
                    }

                    string safeDbName = dbName.Replace("]", "]]");
                    string backupSqlWithCompression = $"BACKUP DATABASE [{safeDbName}] TO DISK = @Disk WITH INIT, STATS = 10, COMPRESSION";
                    string backupSqlPlain = $"BACKUP DATABASE [{safeDbName}] TO DISK = @Disk WITH INIT, STATS = 10";

                    try
                    {
                        await connection.ExecuteAsync(backupSqlWithCompression, new { Disk = fullPath }, commandTimeout: BackupCommandTimeoutSeconds);
                    }
                    catch (SqlException ex) when (ex.Message.IndexOf("compression", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        TryDeleteFile(fullPath);
                        await connection.ExecuteAsync(backupSqlPlain, new { Disk = fullPath }, commandTimeout: BackupCommandTimeoutSeconds);
                    }

                    if (!File.Exists(fullPath))
                        throw new InvalidOperationException("SQL Server reported success but the backup file was not found on disk.");

                    long fileSize = new FileInfo(fullPath).Length;
                    if (fileSize <= 0)
                        throw new InvalidOperationException("The backup file was created but is empty.");

                    await connection.ExecuteAsync("RESTORE VERIFYONLY FROM DISK = @Disk", new { Disk = fullPath }, commandTimeout: BackupCommandTimeoutSeconds);

                    DateTime completedAt = DateTime.Now;
                    await repository.UpdateHistoryResultAsync(historyId, completedAt, fileSize, "Success", null);

                    DateTime? next = ComputeNextBackupTime(completedAt, settings.FrequencyMinutes, true);
                    await repository.UpdateScheduleStateAsync(settings.ID, completedAt, next, "Success", null);

                    await repository.InsertAuditLogAsync("Backup", "Success",
                        $"Full database backup created: {fileName} ({FormatFileSize(fileSize)}), triggered by {triggerType}.", performedBy);

                    await RunRetentionCleanupAsync(settings, performedBy);

                    return new BackupOperationResultDto
                    {
                        Success = true,
                        HistoryId = historyId,
                        BackupFileName = fileName,
                        FileSizeBytes = fileSize,
                        FileSizeFormatted = FormatFileSize(fileSize),
                        Message = $"Backup completed successfully. File: {fileName} ({FormatFileSize(fileSize)})."
                    };
                }
            }
            catch (Exception ex)
            {
                string safeError = SanitizeErrorMessage(ex.Message, folder);

                TryDeleteFile(fullPath);

                await repository.UpdateHistoryResultAsync(historyId, DateTime.Now, null, "Failed", safeError);

                DateTime? nextRetry = ComputeNextBackupTime(DateTime.Now, settings.FrequencyMinutes, settings.IsEnabled);
                await repository.UpdateScheduleStateAsync(settings.ID, settings.LastBackupTime, nextRetry, "Error", safeError);

                await repository.InsertAuditLogAsync("Backup", "Failed",
                    $"Backup failed ({triggerType}): {safeError}", performedBy);

                return new BackupOperationResultDto
                {
                    Success = false,
                    HistoryId = historyId,
                    Message = "Backup failed: " + safeError
                };
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Partial file cleanup is best effort only.
            }
        }

        #endregion Backup

        #region Retention cleanup

        public static async Task<(int DeletedCount, string Warning)> RunRetentionCleanupAsync(BackupSetting settings, string performedBy)
        {
            var repository = new BackupSettingsRepository();

            if (settings == null || settings.RetentionDays <= 0)
                return (0, null);

            try
            {
                string folder = string.IsNullOrWhiteSpace(settings.BackupFolderPath)
                    ? BackupSettingsRepository.GetDefaultBackupFolder()
                    : settings.BackupFolderPath.Trim();

                DateTime cutoff = DateTime.Now.AddDays(-settings.RetentionDays);
                var expired = await repository.GetExpiredSuccessBackupsAsync(cutoff);
                var latest = await repository.GetLatestSuccessAsync();

                int deleted = 0;
                int skippedLocked = 0;

                foreach (var entry in expired)
                {
                    if (latest != null && entry.ID == latest.ID)
                        continue;

                    if (!IsPathInsideFolder(folder, entry.BackupFilePath))
                        continue;

                    try
                    {
                        if (File.Exists(entry.BackupFilePath))
                        {
                            DateTime lastWrite = File.GetLastWriteTime(entry.BackupFilePath);
                            if (lastWrite >= cutoff)
                                continue;

                            File.Delete(entry.BackupFilePath);
                        }

                        await repository.MarkHistoryDeletedAsync(entry.ID, $"Removed by retention policy ({settings.RetentionDays} days).");
                        deleted++;
                    }
                    catch (IOException)
                    {
                        skippedLocked++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        skippedLocked++;
                    }
                    catch (Exception ex)
                    {
                        skippedLocked++;
                        Console.WriteLine(SanitizeErrorMessage(ex.Message, folder));
                    }
                }

                string warning = null;
                if (deleted > 0)
                    await repository.InsertAuditLogAsync("Cleanup", "Success",
                        $"Retention cleanup removed {deleted} backup file(s) older than {settings.RetentionDays} days.", performedBy);

                if (skippedLocked > 0)
                {
                    warning = $"{skippedLocked} old backup file(s) could not be deleted (in use or access denied). They were kept.";
                    await repository.InsertAuditLogAsync("Cleanup", "Warning", warning, performedBy);
                }

                return (deleted, warning);
            }
            catch (Exception ex)
            {
                string safeError = SanitizeErrorMessage(ex.Message);
                await repository.InsertAuditLogAsync("Cleanup", "Failed", "Retention cleanup failed: " + safeError, performedBy);
                return (0, "Retention cleanup failed; old backups were kept. " + safeError);
            }
        }

        #endregion Retention cleanup

        #region Validation & restore

        public static async Task<BackupValidationInfoDto> ValidateBackupFileAsync(string filePath, string expectedDatabaseName)
        {
            var info = new BackupValidationInfoDto();

            try
            {
                if (!File.Exists(filePath))
                {
                    info.Message = "The backup file no longer exists on disk.";
                    return info;
                }

                using (var connection = new SqlConnection(BuildMasterConnectionString()))
                {
                    await connection.OpenAsync();

                    var header = await connection.QueryFirstOrDefaultAsync<RestoreHeaderRow>(
                        "RESTORE HEADERONLY FROM DISK = @Disk", new { Disk = filePath }, commandTimeout: 600);

                    if (header == null)
                    {
                        info.Message = "SQL Server could not read the backup file header. The file may be corrupt or not a valid SQL Server backup.";
                        return info;
                    }

                    string sourceDatabase = header.DatabaseName;
                    DateTime? backupStart = header.BackupStartDate;

                    if (!string.IsNullOrWhiteSpace(expectedDatabaseName) &&
                        !string.Equals(sourceDatabase, expectedDatabaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        info.Message = $"This backup belongs to database '{sourceDatabase}' and cannot be used to restore '{expectedDatabaseName}'.";
                        return info;
                    }

                    await connection.ExecuteAsync("RESTORE VERIFYONLY FROM DISK = @Disk", new { Disk = filePath }, commandTimeout: BackupCommandTimeoutSeconds);

                    info.IsValid = true;
                    info.DatabaseName = sourceDatabase;
                    info.BackupStartDate = backupStart;
                    info.Message = "Backup file verified successfully.";
                    return info;
                }
            }
            catch (Exception ex)
            {
                info.Message = "Backup validation failed: " + SanitizeErrorMessage(ex.Message);
                return info;
            }
        }

        public static async Task<BackupOperationResultDto> RestoreBackupAsync(int historyId, string confirmationName, string performedBy)
        {
            if (!await OperationLock.WaitAsync(0))
            {
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "Another backup or restore operation is already in progress. Please try again shortly."
                };
            }

            Volatile.Write(ref _isOperationRunning, 1);
            try
            {
                return await RestoreBackupCoreAsync(historyId, confirmationName, performedBy);
            }
            finally
            {
                Volatile.Write(ref _isOperationRunning, 0);
                OperationLock.Release();
            }
        }

        private static async Task<BackupOperationResultDto> RestoreBackupCoreAsync(int historyId, string confirmationName, string performedBy)
        {
            var repository = new BackupSettingsRepository();

            // "FORCE RESTORE" is an explicit second confirmation that skips the
            // safety backup. It is only honored after the normal safety backup
            // attempt failed (e.g. the current database is too corrupt to back up).
            bool forceWithoutSafetyBackup = string.Equals(confirmationName?.Trim(), "FORCE RESTORE", StringComparison.OrdinalIgnoreCase);

            BackupSetting settings = await repository.GetSettingsAsync();
            string dbName = GetDatabaseName();

            if (string.IsNullOrWhiteSpace(dbName))
            {
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "The database name could not be determined from the application configuration."
                };
            }

            if (!forceWithoutSafetyBackup &&
                (string.IsNullOrWhiteSpace(confirmationName) ||
                 !string.Equals(confirmationName.Trim(), dbName, StringComparison.OrdinalIgnoreCase)))
            {
                await repository.InsertAuditLogAsync("Restore", "Failed",
                    $"Restore rejected: confirmation name did not match the database name (history ID {historyId}).", performedBy);
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = $"Restore was not started. Type the exact database name '{dbName}' to confirm."
                };
            }

            BackupHistoryEntry entry = await repository.GetHistoryByIdAsync(historyId);
            if (entry == null)
            {
                return new BackupOperationResultDto { Success = false, Message = "The selected backup record was not found." };
            }

            if (!string.Equals(entry.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return new BackupOperationResultDto { Success = false, Message = "Only successful backups can be restored." };
            }

            string folder = string.IsNullOrWhiteSpace(settings?.BackupFolderPath)
                ? BackupSettingsRepository.GetDefaultBackupFolder()
                : settings.BackupFolderPath.Trim();

            if (!IsPathInsideFolder(folder, entry.BackupFilePath))
            {
                await repository.InsertAuditLogAsync("Restore", "Failed",
                    $"Restore rejected: backup file path is outside the configured backup folder (history ID {historyId}).", performedBy);
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "The backup file is outside the configured backup folder and cannot be restored."
                };
            }

            if (!File.Exists(entry.BackupFilePath))
            {
                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "The backup file no longer exists on disk. It may have been deleted or moved."
                };
            }

            var validation = await ValidateBackupFileAsync(entry.BackupFilePath, dbName);
            if (!validation.IsValid)
            {
                await repository.InsertAuditLogAsync("Restore", "Failed",
                    $"Restore validation failed for history ID {historyId}: {validation.Message}", performedBy);
                return new BackupOperationResultDto { Success = false, Message = validation.Message };
            }

            // Safety backup of the current database before overwriting anything.
            BackupOperationResultDto safetyBackup = null;
            if (!forceWithoutSafetyBackup)
            {
                safetyBackup = await RunBackupCoreAsync("PreRestore", performedBy);
                if (!safetyBackup.Success)
                {
                    await repository.InsertAuditLogAsync("Restore", "Failed",
                        $"Restore aborted: safety backup failed ({safetyBackup.Message}).", performedBy);
                    return new BackupOperationResultDto
                    {
                        Success = false,
                        Message = "Restore was aborted because the automatic safety backup of the current database failed: " + safetyBackup.Message
                                  + " If the current database cannot be backed up (for example because it is corrupt), you may force the restore by confirming again with the exact text FORCE RESTORE."
                    };
                }
            }

            string safeDbName = dbName.Replace("]", "]]");
            bool singleUserSet = false;

            try
            {
                using (var connection = new SqlConnection(BuildMasterConnectionString()))
                {
                    await connection.OpenAsync();

                    int lockResult = await connection.ExecuteScalarAsync<int>(
                        @"DECLARE @res INT;
                          EXEC @res = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0;
                          SELECT @res;",
                        new { Resource = AppLockResource });

                    if (lockResult < 0)
                        throw new InvalidOperationException("Another backup or restore operation is already running on the server.");

                    await connection.ExecuteAsync(
                        $"ALTER DATABASE [{safeDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                        commandTimeout: 300);
                    singleUserSet = true;

                    await connection.ExecuteAsync(
                        $"RESTORE DATABASE [{safeDbName}] FROM DISK = @Disk WITH REPLACE, RECOVERY, STATS = 10",
                        new { Disk = entry.BackupFilePath },
                        commandTimeout: BackupCommandTimeoutSeconds);

                    await connection.ExecuteAsync(
                        $"ALTER DATABASE [{safeDbName}] SET MULTI_USER",
                        commandTimeout: 300);
                    singleUserSet = false;
                }

                string safetyNote = forceWithoutSafetyBackup
                    ? "Restore was forced without a safety backup after an explicit confirmation."
                    : $"Safety backup: {safetyBackup.BackupFileName}.";

                await repository.InsertAuditLogAsync("Restore", "Success",
                    $"Database restored from backup '{entry.BackupFileName}' (history ID {historyId}). {safetyNote}",
                    performedBy);

                string message = forceWithoutSafetyBackup
                    ? $"Database restored successfully from '{entry.BackupFileName}' without a safety backup (explicitly forced). Active sessions were disconnected during the restore."
                    : $"Database restored successfully from '{entry.BackupFileName}'. A safety backup of the previous data was created as '{safetyBackup.BackupFileName}'. Active sessions were disconnected during the restore.";

                return new BackupOperationResultDto
                {
                    Success = true,
                    BackupFileName = entry.BackupFileName,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                string safeError = SanitizeErrorMessage(ex.Message, folder);

                if (singleUserSet)
                {
                    try
                    {
                        using (var connection = new SqlConnection(BuildMasterConnectionString()))
                        {
                            await connection.OpenAsync();
                            await connection.ExecuteAsync(
                                $"ALTER DATABASE [{safeDbName}] SET MULTI_USER",
                                commandTimeout: 300);
                        }
                    }
                    catch
                    {
                        // Best effort to bring the database back online.
                    }
                }

                await repository.InsertAuditLogAsync("Restore", "Failed",
                    $"Restore failed for history ID {historyId}: {safeError}", performedBy);

                return new BackupOperationResultDto
                {
                    Success = false,
                    Message = "Restore failed: " + safeError
                };
            }
        }

        #endregion Validation & restore

        private class RestoreHeaderRow
        {
            public string DatabaseName { get; set; }
            public DateTime? BackupStartDate { get; set; }
        }
    }
}
