using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class RestoreDatabaseInputDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ID { get; set; }

        [Required]
        public string ConfirmationName { get; set; }
    }
}
