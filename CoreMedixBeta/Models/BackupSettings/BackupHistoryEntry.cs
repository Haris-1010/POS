using InstaCareEMR.Core.DBHelpers;
using System;

namespace CoreMedixBeta.Models.BackupSettings
{
    [Schema("Settings")]
    [Table("BackupHistory")]
    public class BackupHistoryEntry
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string BackupFileName { get; set; }
        public string BackupFilePath { get; set; }
        public DateTime BackupStartTime { get; set; }
        public DateTime? BackupEndTime { get; set; }
        public long? FileSizeBytes { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string TriggerType { get; set; }
        public string CreatedBy { get; set; }
    }
}
