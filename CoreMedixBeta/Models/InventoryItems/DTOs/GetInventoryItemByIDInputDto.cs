using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetInventoryItemByIDInputDto
    {
        [Required]
        public string ID { get; set; }
    }
}