using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases.DTOs
{
    public class ManagePurchaseInputDto
    {
        public int ID { get; set; } = 0;
        public int PurchaseOrderID { get; set; } = 0;
    }
}