using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventorySettings.DTOs
{
    public class SaveItemAttributeInputDto
    {
        public int? ID { get; set; } = 0;

        public string Title { get; set; }

        [Required]
        public string Type { get; set; }
    }
}