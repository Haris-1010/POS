using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases.DTOs
{
    public class SavePurchaseInputDto
    {
        public Purchase Purchase { get; set; }
        public List<PurchaseItem> PurchaseItems { get; set; }
        public List<PurchasePayment> PurchasePayments { get; set; }
        public string CurrentUsername { get; set; }
    }
}