using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.InventorySettings.DTOs
{
    public class DeleteItemAttributeInputDto
    {
        [Required]
        public int ID { get; set; }
    }
}