using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class SaveBackupSettingInputDto
    {
        public int ID { get; set; }

        [Required]
        public bool IsEnabled { get; set; }

        [Required]
        [Range(5, 10080)]
        public int FrequencyMinutes { get; set; }

        [Required]
        [StringLength(500)]
        public string BackupFolderPath { get; set; }

        [Required]
        [Range(0, 3650)]
        public int RetentionDays { get; set; }
    }
}
