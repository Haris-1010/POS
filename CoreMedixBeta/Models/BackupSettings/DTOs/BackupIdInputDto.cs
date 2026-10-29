using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class BackupIdInputDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ID { get; set; }
    }
}
