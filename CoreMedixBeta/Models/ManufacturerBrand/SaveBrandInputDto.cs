using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.ManufacturerBrand
{
    public class SaveBrandInputDto
    {
        public int? ID { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string ManufacturerID { get; set; }
    }
}