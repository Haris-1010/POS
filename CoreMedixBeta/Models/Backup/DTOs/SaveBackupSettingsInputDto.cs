using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.Backup.DTOs
{
    public class SaveBackupSettingsInputDto
    {
        public bool IsEnabled { get; set; }

        [Range(1, 10080, ErrorMessage = "Backup interval must be between 1 and 10080 minutes.")]
        public int IntervalMinutes { get; set; } = 60;

        [Required]
        [StringLength(400, ErrorMessage = "Backup folder path cannot exceed 400 characters.")]
        public string BackupFolder { get; set; }

        [Range(0, 3650, ErrorMessage = "Retention must be between 0 and 3650 days.")]
        public int RetentionDays { get; set; } = 30;
    }
}
