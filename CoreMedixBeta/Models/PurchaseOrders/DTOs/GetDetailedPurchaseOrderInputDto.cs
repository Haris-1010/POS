using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class GetDetailedPurchaseOrderInputDto
    {
        public int ID { get; set; }
        public bool IsPrint { get; set; } = false;
    }
}