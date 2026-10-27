using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class DeleteInventoryItemInputDto
    {
        [Required]
        public int ID { get; set; }

        public string DeletedBy { get; set; }
    }
}