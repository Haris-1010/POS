using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.Menufacturer.DTOs
{
    public class DeleteManufacturerInputDto
    {
        [Required]
        public int ID { get; set; }

        public string DeletedBy { get; set; }
    }
}