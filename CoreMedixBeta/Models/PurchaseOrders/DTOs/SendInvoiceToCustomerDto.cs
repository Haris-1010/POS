using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class SendInvoiceToCustomerDto
    {
        [Required]
        public int ID { get; set; }

        [Required]
        public string ReceiverName { get; set; }

        [Required]
        public string ReceiverPhone { get; set; }

        public string SenderName { get; set; } = string.Empty;
    }
}