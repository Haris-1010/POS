using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventorySettings.DTOs
{
    public class DeleteItemAttributeInputDto
    {
        [Required]
        public int ID { get; set; }
    }
}