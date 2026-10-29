using System;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class GetPagedBackupHistoryOutputDto
    {
        public int ID { get; set; }
        public string BackupFileName { get; set; }
        public string BackupStartTime { get; set; }
        public string BackupEndTime { get; set; }
        public string FileSize { get; set; }
        public string Status { get; set; }
        public string TriggerType { get; set; }
        public string CreatedBy { get; set; }
        public string ErrorMessage { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
