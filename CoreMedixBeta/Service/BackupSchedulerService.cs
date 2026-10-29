using CoreMedixBeta.Models.BackupSettings;
using CoreMedixBeta.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace CoreMedixBeta.Service
{
    /// <summary>
    /// In-process scheduler that wakes up once a minute and runs a database
    /// backup whenever the interval saved in Settings.BackupSettings says one is
    /// due. It is registered with IIS so it shuts down cleanly on application
    /// pool recycles.
    ///
    /// This timer is intentionally NOT the only safety net: the
    /// Database/Setup-BackupScheduledTask.ps1 script registers a Windows
    /// Scheduled Task that calls BackupSettings/ScheduledBackup. That endpoint
    /// runs this same due-check, so scheduling keeps working even if IIS is
    /// stopped or the application pool is recycled for long periods. Duplicate
    /// jobs are prevented by the due-check itself, an in-process lock and a
    /// SQL Server application lock.
    /// </summary>
    public static class BackupSchedulerService
    {
        private static readonly object Sync = new object();
        private static Timer _timer;
        private static int _started;
        private static int _tickRunning;

        public static void Start()
        {
            lock (Sync)
            {
                if (_started == 1) return;
                _started = 1;

                try
                {
                    HostingEnvironment.RegisterObject(new ShutdownHandler());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BackupSchedulerService: could not register shutdown handler: " + ex.Message);
                }

                _timer = new Timer(_ => OnTick(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                try
                {
                    _timer?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BackupSchedulerService: " + ex.Message);
                }

                _timer = null;
                _started = 0;
            }
        }

        private static void OnTick()
        {
            if (Interlocked.CompareExchange(ref _tickRunning, 1, 0) != 0)
                return;

            try
            {
                HostingEnvironment.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        if (!token.IsCancellationRequested)
                            await CheckAndRunDueBackupAsync("Auto", "System");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("BackupSchedulerService tick error: " + ex.Message);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _tickRunning, 0);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("BackupSchedulerService: " + ex.Message);
                Interlocked.Exchange(ref _tickRunning, 0);
            }
        }

        /// <summary>
        /// Runs a backup if the saved schedule says one is due. Safe to call
        /// from the in-process timer, the Windows Scheduled Task endpoint and
        /// never runs two jobs at once.
        /// </summary>
        public static async Task<string> CheckAndRunDueBackupAsync(string triggeredBy, string performedBy = null)
        {
            var repository = new BackupSettingsRepository();

            BackupSetting settings = await repository.GetSettingsAsync();
            if (settings == null || settings.ID == 0)
                return "Backup settings are not available.";

            if (!settings.IsEnabled)
            {
                if (settings.NextBackupTime != null || string.Equals(settings.LastStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
                {
                    await repository.UpdateScheduleStateAsync(settings.ID, settings.LastBackupTime, null, "Disabled", null);
                }
                return "Automatic backup is disabled.";
            }

            if (settings.FrequencyMinutes < 1440)
                settings.FrequencyMinutes = 1440;

            DateTime now = DateTime.Now;

            if (settings.NextBackupTime == null)
            {
                DateTime computed = BackupService.ComputeNextBackupTime(settings.LastBackupTime, settings.FrequencyMinutes, true) ?? now.AddMinutes(settings.FrequencyMinutes);

                if (computed > now)
                {
                    await repository.UpdateScheduleStateAsync(settings.ID, settings.LastBackupTime, computed, "Scheduled", null);
                    return $"Backup scheduled for {computed:yyyy-MM-dd HH:mm:ss}.";
                }

                settings.NextBackupTime = computed;
            }

            if (settings.NextBackupTime > now)
                return $"Next backup is scheduled for {settings.NextBackupTime.Value:yyyy-MM-dd HH:mm:ss}.";

            var result = await BackupService.RunBackupAsync(triggeredBy, string.IsNullOrWhiteSpace(performedBy) ? triggeredBy : performedBy);
            return result.Message;
        }

        private class ShutdownHandler : IRegisteredObject
        {
            public void Stop(bool immediate)
            {
                BackupSchedulerService.Stop();
                HostingEnvironment.UnregisterObject(this);
            }
        }
    }
}
