using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class SavePurchaseOrderInputDto
    {
        public PurchaseOrder Order { get; set; }
        public List<PurchaseOrderItem> OrderItems { get; set; }
        public string CurrentUsername { get; set; }
    }
}