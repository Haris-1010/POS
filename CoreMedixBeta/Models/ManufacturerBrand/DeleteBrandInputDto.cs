using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.ManufacturerBrand
{
    public class DeleteBrandInputDto
    {
        [Required]
        public int ID { get; set; }
    }
}