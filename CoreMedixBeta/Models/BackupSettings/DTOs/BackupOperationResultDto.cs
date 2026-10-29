namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class BackupOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string BackupFileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public string FileSizeFormatted { get; set; }
        public int HistoryId { get; set; }
    }
}
