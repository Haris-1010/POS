using System.ComponentModel.DataAnnotations;

namespace HARTPOS.Models.Supplier
{
    public class DeleteSupplierInputDto
    {
        [Required]
        public int SupplierID { get; set; }

        public string DeletedBy { get; set; }
    }
}