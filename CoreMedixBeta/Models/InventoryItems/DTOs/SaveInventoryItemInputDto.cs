using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class SaveInventoryItemInputDto
    {
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ProductType { get; set; }

        public string MedicineSalt { get; set; }
        public int? BrandID { get; set; } = 0;

        [Required]
        public int ManufacturerID { get; set; }

        [Required]
        public string SupplierID { get; set; }

        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }
}