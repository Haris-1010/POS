using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class TestBackupFolderInputDto
    {
        [Required]
        [StringLength(500)]
        public string BackupFolderPath { get; set; }
    }
}
