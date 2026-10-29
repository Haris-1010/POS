using InstaCareEMR.Core.DBHelpers;
using System;

namespace CoreMedixBeta.Models.BackupSettings
{
    [Schema("Settings")]
    [Table("BackupSettings")]
    public class BackupSetting
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public bool IsEnabled { get; set; }
        public int FrequencyMinutes { get; set; }
        public string BackupFolderPath { get; set; }
        public int RetentionDays { get; set; }
        public DateTime? LastBackupTime { get; set; }
        public DateTime? NextBackupTime { get; set; }
        public string LastStatus { get; set; }
        public string LastErrorMessage { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
    }
}
