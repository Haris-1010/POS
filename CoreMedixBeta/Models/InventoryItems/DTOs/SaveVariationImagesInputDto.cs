using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class SaveVariationImagesInputDto
    {
        [Required]
        public string ImageName { get; set; }

        [Required]
        public string ImageURL { get; set; }

        [Required]
        public int VariationID { get; set; }
    }
}