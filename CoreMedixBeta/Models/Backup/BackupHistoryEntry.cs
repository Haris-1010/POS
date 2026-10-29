using System;

namespace CoreMedixBeta.Models.Backup
{
    /// <summary>
    /// Row of dbo.BackupHistory - one record per backup attempt.
    /// Mapped manually by CoreMedixBeta.Services.DatabaseBackupEngine (raw SqlClient).
    /// </summary>
    public class BackupHistoryEntry
    {
        public long ID { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string DatabaseName { get; set; }
        /// <summary>Scheduled | Manual | PreRestore</summary>
        public string BackupType { get; set; }
        /// <summary>Running | Success | Failed</summary>
        public string Status { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string ErrorMessage { get; set; }
        public string CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        /// <summary>Manual | Retention</summary>
        public string DeletedReason { get; set; }
        public DateTime? RestoredOn { get; set; }
        public string RestoredBy { get; set; }
    }
}
