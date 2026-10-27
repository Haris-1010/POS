using CoreMedixBeta.Models.Purchases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class ClearDueAmountInputDto
    {
        public int ID { get; set; }
        public decimal RemainingAmount { get; set; }
        public PurchasePayment PurchasePayment { get; set; }
        public string LastModifiedBy { get; set; }
    }
}