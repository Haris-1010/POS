using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class CreateSubfolderInputDto
    {
        [Required]
        public string Path { get; set; }

        [Required]
        [StringLength(255)]
        public string FolderName { get; set; }
    }
}
