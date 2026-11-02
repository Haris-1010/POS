using System.ComponentModel.DataAnnotations;

namespace HARTPOS.Models.ManufacturerBrand
{
    public class DeleteBrandInputDto
    {
        [Required]
        public int ID { get; set; }
    }
}