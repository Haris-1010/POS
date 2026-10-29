using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class SaveBackupSettingInputDto
    {
        public int ID { get; set; }

        [Required]
        public bool IsEnabled { get; set; }

        [Required]
        [Range(1440, 525600)] // 1 day .. 365 days, stored in minutes (UI offers day/month presets)
        public int FrequencyMinutes { get; set; }

        [Required]
        [StringLength(500)]
        public string BackupFolderPath { get; set; }

        [Required]
        [Range(0, 3650)]
        public int RetentionDays { get; set; }
    }
}
