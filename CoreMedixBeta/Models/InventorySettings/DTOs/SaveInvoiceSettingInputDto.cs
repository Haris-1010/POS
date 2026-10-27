using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventorySettings.DTOs
{
    public class SaveInvoiceSettingInputDto
    {
        public int ID { get; set; }

        [Required]
        public string InvoicePrefix { get; set; }

        [Required]
        public string Currency { get; set; }

        [Required]
        public string UserName { get; set; }
    }
}