using System;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class BackupValidationInfoDto
    {
        public bool IsValid { get; set; }
        public string DatabaseName { get; set; }
        public DateTime? BackupStartDate { get; set; }
        public string Message { get; set; }
    }
}
