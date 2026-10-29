using System;

namespace CoreMedixBeta.Models.Backup
{
    /// <summary>
    /// Row of dbo.BackupSettings (singleton configuration row).
    /// Mapped manually by CoreMedixBeta.Services.DatabaseBackupEngine (raw SqlClient)
    /// so the same code can run from the scheduled-task runner without Dapper/MVC.
    /// </summary>
    public class DatabaseBackupSetting
    {
        public int ID { get; set; }
        public bool IsEnabled { get; set; }
        public int IntervalMinutes { get; set; } = 60;
        public string BackupFolder { get; set; }
        public int RetentionDays { get; set; } = 30;
        public DateTime? LastSuccessTime { get; set; }
        public DateTime? LastAttemptTime { get; set; }
        public string LastStatus { get; set; } = "Never Run";
        public string LastError { get; set; }
        public string LastBackupFile { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
