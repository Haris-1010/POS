using CoreMedixBeta.DbModels;
using CoreMedixBeta.Filters;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.BackupSettings;
using CoreMedixBeta.Models.BackupSettings.DTOs;
using CoreMedixBeta.Repositories;
using CoreMedixBeta.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class BackupSettingsController : Controller
    {
        private readonly BackupSettingsRepository _backupSettingsRepository;

        public BackupSettingsController() : this(new BackupSettingsRepository())
        {
        }

        public BackupSettingsController(BackupSettingsRepository backupSettingsRepository)
        {
            _backupSettingsRepository = backupSettingsRepository;
        }

        #region Settings page

        [AuthorizePermission("InventorySettings_View", "BackupSettings_View")]
        public async Task<ActionResult> Index()
        {
            BackupSettingsVM viewModel = await BuildSettingsViewModelAsync();
            return View(viewModel);
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_View", "BackupSettings_View")]
        public async Task<JsonResult> GetStatusSummary()
        {
            try
            {
                BackupSettingsVM viewModel = await BuildSettingsViewModelAsync();
                return Json(new AjaxRequestDTO { Status = true, Message = "Status refreshed", Data = viewModel });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to load backup status." });
            }
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage", "BackupSettings_Manage")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveSettings(SaveBackupSettingInputDto formInput)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid backup settings data." });

                string folderPath = formInput.BackupFolderPath?.Trim();
                if (string.IsNullOrWhiteSpace(folderPath))
                    return Json(new AjaxRequestDTO { Status = false, Message = "Backup folder path is required." });

                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(folderPath);
                }
                catch (Exception)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "The backup folder path is not a valid Windows path." });
                }

                var folderCheck = BackupService.ValidateFolder(fullPath);
                if (!folderCheck.Success)
                    return Json(new AjaxRequestDTO { Status = false, Message = folderCheck.Message });

                int frequency = Math.Max(5, Math.Min(10080, formInput.FrequencyMinutes));
                int retention = Math.Max(0, Math.Min(3650, formInput.RetentionDays));

                BackupSetting existing = await _backupSettingsRepository.GetSettingsAsync();

                BackupSetting settings = new BackupSetting
                {
                    ID = existing?.ID ?? 0,
                    IsEnabled = formInput.IsEnabled,
                    FrequencyMinutes = frequency,
                    BackupFolderPath = fullPath,
                    RetentionDays = retention,
                    LastBackupTime = existing?.LastBackupTime,
                    LastErrorMessage = null,
                    LastModifiedBy = Session["Username"]?.ToString()
                };

                BackupSetting saved = await _backupSettingsRepository.SaveSettingsAsync(settings);
                if (saved == null || saved.ID <= 0)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Failed to save backup settings due to a technical error." });

                DateTime? next = BackupService.ComputeNextBackupTime(saved.LastBackupTime, saved.FrequencyMinutes, saved.IsEnabled);
                string status = saved.IsEnabled ? "Scheduled" : "Disabled";
                await _backupSettingsRepository.UpdateScheduleStateAsync(saved.ID, saved.LastBackupTime, next, status, null);

                await _backupSettingsRepository.InsertAuditLogAsync("SettingsChange", "Success",
                    $"Backup settings updated. Enabled: {saved.IsEnabled}, every {saved.FrequencyMinutes} minute(s), retention {saved.RetentionDays} day(s), folder: {fullPath}.",
                    Session["Username"]?.ToString());

                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Message = saved.IsEnabled
                        ? $"Backup settings saved. Automatic backups will run every {saved.FrequencyMinutes} minute(s)."
                        : "Backup settings saved. Automatic backups are currently disabled."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to save backup settings due to a technical error." });
            }
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage", "BackupSettings_Manage")]
        public async Task<JsonResult> TestBackupFolder(TestBackupFolderInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "A folder path is required." });

                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(input.BackupFolderPath?.Trim());
                }
                catch (Exception)
                {
                    return Json(new AjaxRequestDTO { Status = false, Message = "The backup folder path is not a valid Windows path." });
                }

                var folderCheck = BackupService.ValidateFolder(fullPath);

                await _backupSettingsRepository.InsertAuditLogAsync("FolderTest", folderCheck.Success ? "Success" : "Failed",
                    $"Folder test for '{fullPath}': {folderCheck.Message}", Session["Username"]?.ToString());

                return Json(new AjaxRequestDTO
                {
                    Status = folderCheck.Success,
                    Message = folderCheck.Message,
                    Data = new { Path = fullPath, Created = folderCheck.Created }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to test the backup folder." });
            }
        }

        #endregion Settings page

        #region Manual backup

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage", "BackupSettings_Manage")]
        public async Task<JsonResult> RunBackupNow()
        {
            try
            {
                var result = await BackupService.RunBackupAsync("Manual", Session["Username"]?.ToString());

                return Json(new AjaxRequestDTO
                {
                    Status = result.Success,
                    Message = result.Message,
                    Data = new
                    {
                        result.HistoryId,
                        result.BackupFileName,
                        result.FileSizeFormatted
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to start the backup due to a technical error." });
            }
        }

        #endregion Manual backup

        #region History

        [HttpPost]
        [AuthorizePermission("InventorySettings_View", "BackupSettings_View")]
        public async Task<JsonResult> GetPagedBackupHistory(GetPagedBackupHistoryInputDto input)
        {
            try
            {
                input.DataTableValues = Common.GetMappedDataTableValues(HttpContext);
                var data = await _backupSettingsRepository.GetPagedBackupHistoryAsync(input);

                return Json(new PagedAjaxRequestOutputDTO
                {
                    draw = Request.Form.GetValues("draw")?.FirstOrDefault() ?? "1",
                    recordsFiltered = data?.Item2 ?? "0",
                    recordsTotal = data?.Item2 ?? "0",
                    data = data?.Item1 ?? "[]"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new PagedAjaxRequestOutputDTO());
            }
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_View", "BackupSettings_View")]
        public async Task<JsonResult> GetBackupDetails(BackupIdInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid backup record." });

                BackupHistoryEntry entry = await _backupSettingsRepository.GetHistoryByIdAsync(input.ID);
                if (entry == null || string.Equals(entry.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
                    return Json(new AjaxRequestDTO { Status = false, Message = "Backup record not found." });

                var settings = await _backupSettingsRepository.GetSettingsAsync();
                string folder = string.IsNullOrWhiteSpace(settings?.BackupFolderPath)
                    ? BackupSettingsRepository.GetDefaultBackupFolder()
                    : settings.BackupFolderPath.Trim();

                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Message = "Backup details loaded",
                    Data = new
                    {
                        entry.ID,
                        entry.BackupFileName,
                        BackupLocation = BackupService.IsPathInsideFolder(folder, entry.BackupFilePath) ? folder : "Unknown location",
                        BackupStartTime = entry.BackupStartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        BackupEndTime = entry.BackupEndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                        FileSize = BackupService.FormatFileSize(entry.FileSizeBytes),
                        entry.Status,
                        entry.TriggerType,
                        entry.CreatedBy,
                        entry.ErrorMessage
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to load backup details." });
            }
        }

        [HttpPost]
        [AuthorizePermission("InventorySettings_Manage", "BackupSettings_Manage")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteBackup(BackupIdInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid backup record." });

                string username = Session["Username"]?.ToString();

                BackupHistoryEntry entry = await _backupSettingsRepository.GetHistoryByIdAsync(input.ID);
                if (entry == null)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Backup record not found." });

                if (string.Equals(entry.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
                    return Json(new AjaxRequestDTO { Status = false, Message = "This backup was already deleted." });

                if (string.Equals(entry.Status, "Running", StringComparison.OrdinalIgnoreCase))
                    return Json(new AjaxRequestDTO { Status = false, Message = "A running backup cannot be deleted." });

                var latest = await _backupSettingsRepository.GetLatestSuccessAsync();
                if (latest != null && latest.ID == entry.ID)
                    return Json(new AjaxRequestDTO { Status = false, Message = "The most recent successful backup cannot be deleted. Take a new backup first." });

                var settings = await _backupSettingsRepository.GetSettingsAsync();
                string folder = string.IsNullOrWhiteSpace(settings?.BackupFolderPath)
                    ? BackupSettingsRepository.GetDefaultBackupFolder()
                    : settings.BackupFolderPath.Trim();

                if (!BackupService.IsPathInsideFolder(folder, entry.BackupFilePath))
                    return Json(new AjaxRequestDTO { Status = false, Message = "The backup file is outside the configured backup folder and cannot be deleted from here." });

                if (System.IO.File.Exists(entry.BackupFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(entry.BackupFilePath);
                    }
                    catch (IOException)
                    {
                        return Json(new AjaxRequestDTO { Status = false, Message = "The backup file is currently in use and could not be deleted." });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return Json(new AjaxRequestDTO { Status = false, Message = "Access to the backup file was denied. Check folder permissions." });
                    }
                }

                await _backupSettingsRepository.MarkHistoryDeletedAsync(entry.ID, $"Deleted manually by {username}");
                await _backupSettingsRepository.InsertAuditLogAsync("Delete", "Success",
                    $"Backup '{entry.BackupFileName}' (history ID {entry.ID}) was deleted.", username);

                return Json(new AjaxRequestDTO { Status = true, Message = "Backup deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to delete the backup due to a technical error." });
            }
        }

        [AuthorizePermission("InventorySettings_Manage", "BackupSettings_Manage")]
        public async Task<ActionResult> DownloadBackup(int id)
        {
            try
            {
                BackupHistoryEntry entry = await _backupSettingsRepository.GetHistoryByIdAsync(id);
                if (entry == null || string.Equals(entry.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index");

                var settings = await _backupSettingsRepository.GetSettingsAsync();
                string folder = string.IsNullOrWhiteSpace(settings?.BackupFolderPath)
                    ? BackupSettingsRepository.GetDefaultBackupFolder()
                    : settings.BackupFolderPath.Trim();

                if (!BackupService.IsPathInsideFolder(folder, entry.BackupFilePath))
                    return RedirectToAction("Index");

                if (!System.IO.File.Exists(entry.BackupFilePath))
                {
                    TempData["BackupError"] = "The backup file no longer exists on disk.";
                    return RedirectToAction("Index");
                }

                await _backupSettingsRepository.InsertAuditLogAsync("Download", "Success",
                    $"Backup '{entry.BackupFileName}' (history ID {entry.ID}) was downloaded.", Session["Username"]?.ToString());

                return File(entry.BackupFilePath, "application/octet-stream", entry.BackupFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return RedirectToAction("Index");
            }
        }

        #endregion History

        #region Restore

        [HttpPost]
        [AuthorizePermission("BackupSettings_Restore")]
        public async Task<JsonResult> ValidateRestore(BackupIdInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid backup record." });

                BackupHistoryEntry entry = await _backupSettingsRepository.GetHistoryByIdAsync(input.ID);
                if (entry == null || !string.Equals(entry.Status, "Success", StringComparison.OrdinalIgnoreCase))
                    return Json(new AjaxRequestDTO { Status = false, Message = "Only successful backups can be restored." });

                var settings = await _backupSettingsRepository.GetSettingsAsync();
                string folder = string.IsNullOrWhiteSpace(settings?.BackupFolderPath)
                    ? BackupSettingsRepository.GetDefaultBackupFolder()
                    : settings.BackupFolderPath.Trim();

                if (!BackupService.IsPathInsideFolder(folder, entry.BackupFilePath))
                    return Json(new AjaxRequestDTO { Status = false, Message = "The backup file is outside the configured backup folder and cannot be restored." });

                string dbName = await _backupSettingsRepository.GetDatabaseNameAsync();
                var validation = await BackupService.ValidateBackupFileAsync(entry.BackupFilePath, dbName);

                return Json(new AjaxRequestDTO
                {
                    Status = validation.IsValid,
                    Message = validation.Message,
                    Data = new
                    {
                        validation.IsValid,
                        validation.DatabaseName,
                        BackupStartDate = validation.BackupStartDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                        CurrentDatabaseName = dbName,
                        entry.BackupFileName
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to validate the backup file." });
            }
        }

        [HttpPost]
        [AuthorizePermission("BackupSettings_Restore")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RestoreDatabase(RestoreDatabaseInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO { Status = false, Message = "Invalid restore request." });

                var result = await BackupService.RestoreBackupAsync(input.ID, input.ConfirmationName, Session["Username"]?.ToString());

                return Json(new AjaxRequestDTO
                {
                    Status = result.Success,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to restore the database due to a technical error." });
            }
        }

        #endregion Restore

        #region Scheduled task endpoint

        /// <summary>
        /// Called by the Windows Scheduled Task (see Scripts/Setup-BackupScheduledTask.ps1).
        /// The shared key is stored in Web.config (BackupSchedulerKey). The task may
        /// fire on a fixed short cadence; this action only runs a backup when the
        /// schedule saved in the database says one is actually due.
        /// </summary>
        [AllowAnonymous]
        public async Task<ActionResult> ScheduledBackup(string key)
        {
            string expectedKey = System.Configuration.ConfigurationManager.AppSettings["BackupSchedulerKey"];

            if (string.IsNullOrWhiteSpace(expectedKey) || !KeysMatch(key, expectedKey))
                return new HttpStatusCodeResult(403, "Forbidden");

            try
            {
                string message = await BackupSchedulerService.CheckAndRunDueBackupAsync("ScheduledTask", "ScheduledTask");
                return Content(message, "text/plain", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new HttpStatusCodeResult(500, "Backup scheduler error");
            }
        }

        private static bool KeysMatch(string provided, string expected)
        {
            if (provided == null) provided = string.Empty;
            byte[] a = Encoding.UTF8.GetBytes(provided);
            byte[] b = Encoding.UTF8.GetBytes(expected);

            if (a.Length != b.Length)
            {
                // Still walk the expected value to keep timing uniform.
                int diff = a.Length ^ b.Length;
                for (int i = 0; i < b.Length; i++) diff |= b[i];
                return false;
            }

            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }

        #endregion Scheduled task endpoint

        #region Helpers

        private async Task<BackupSettingsVM> BuildSettingsViewModelAsync()
        {
            BackupSetting settings = await _backupSettingsRepository.GetSettingsAsync();
            BackupHistoryEntry latest = await _backupSettingsRepository.GetLatestSuccessAsync();
            string dbName = await _backupSettingsRepository.GetDatabaseNameAsync();

            string folder = string.IsNullOrWhiteSpace(settings?.BackupFolderPath)
                ? BackupSettingsRepository.GetDefaultBackupFolder()
                : settings.BackupFolderPath.Trim();

            bool folderExists, folderWritable;
            string freeSpace;
            string folderMessage = BackupService.DescribeFolderStatus(folder, out folderExists, out folderWritable, out freeSpace);

            bool running = BackupService.IsBackupRunning;

            string statusText;
            string badgeClass;
            if (running)
            {
                statusText = "Running";
                badgeClass = "badge-warning";
            }
            else if (settings == null || !settings.IsEnabled)
            {
                statusText = "Disabled";
                badgeClass = "badge-secondary";
            }
            else if (string.Equals(settings.LastStatus, "Error", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "Error";
                badgeClass = "badge-danger";
            }
            else
            {
                statusText = "Scheduled";
                badgeClass = "badge-success";
            }

            return new BackupSettingsVM
            {
                Settings = settings ?? new BackupSetting { BackupFolderPath = folder, FrequencyMinutes = 1440, RetentionDays = 30, LastStatus = "Disabled" },
                StatusText = statusText,
                StatusBadgeClass = badgeClass,
                LastBackupTimeFormatted = settings?.LastBackupTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                NextBackupTimeFormatted = running
                    ? "In progress..."
                    : (settings?.NextBackupTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not scheduled"),
                LastBackupFileName = latest?.BackupFileName ?? "None",
                LastBackupFileSize = BackupService.FormatFileSize(latest?.FileSizeBytes),
                LastSuccessTime = latest?.BackupEndTime ?? latest?.BackupStartTime,
                FolderExists = folderExists,
                FolderWritable = folderWritable,
                FolderError = folderExists && folderWritable ? null : folderMessage,
                FolderFreeSpace = freeSpace,
                DatabaseName = dbName,
                IsBackupRunning = running,
                LastErrorMessage = settings?.LastErrorMessage
            };
        }

        #endregion Helpers
    }
}
