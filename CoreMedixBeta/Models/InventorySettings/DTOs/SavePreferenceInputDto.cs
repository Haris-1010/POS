using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventorySettings.DTOs
{
    public class SavePreferenceInputDto
    {
        public int ID { get; set; }

        [Required]
        public string Type { get; set; }

        public string LastModifiedBy { get; set; }
    }
}