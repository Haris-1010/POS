using System;
using System.Threading;

namespace CoreMedixBeta.Services
{
    /// <summary>
    /// In-process backup scheduler (convenience layer).
    ///
    /// Wakes up once a minute, asks the database whether a backup is due and - if so -
    /// runs one in the background. It never runs a backup on a page request and never
    /// blocks a request: actual work happens on a thread-pool thread.
    ///
    /// This timer is NOT the only scheduling mechanism. IIS can recycle the application
    /// pool at any time, so the Windows Scheduled Task running
    /// CoreMedixBeta.BackupRunner.exe (see Database\Install-BackupScheduledTask.ps1)
    /// is the persistent scheduler that guarantees backups continue after a recycle.
    /// Both mechanisms coordinate through a SQL Server application lock, so they can
    /// never create duplicate or overlapping backups.
    /// </summary>
    public static class BackupScheduler
    {
        private static readonly object Sync = new object();
        private static Timer _timer;
        private static int _tickRunning;
        private static DateTime _lastErrorLogUtc = DateTime.MinValue;

        public static bool IsRunning
        {
            get { return _timer != null; }
        }

        public static void Start()
        {
            lock (Sync)
            {
                if (_timer != null) return;
                // First check after 90 seconds (lets the app warm up), then every minute.
                _timer = new Timer(Tick, null, TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(60));
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (_timer == null) return;
                try { _timer.Dispose(); }
                catch { }
                _timer = null;
            }
        }

        private static void Tick(object state)
        {
            // Never overlap ticks: a long backup simply makes later ticks no-ops.
            if (Interlocked.CompareExchange(ref _tickRunning, 1, 0) != 0) return;
            try
            {
                BackupRunResult result = DatabaseBackupEngine.RunDueBackup("Scheduler (Web)");
                if (!result.Success && !result.Skipped)
                {
                    LogError(result.Message);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _tickRunning, 0);
            }
        }

        private static void LogError(string message)
        {
            // Keep noise down: one line at most every 5 minutes.
            if ((DateTime.UtcNow - _lastErrorLogUtc).TotalMinutes < 5) return;
            _lastErrorLogUtc = DateTime.UtcNow;
            Console.WriteLine("[BackupScheduler] " + message);
        }
    }
}
