using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Purchases.DTOs
{
    public class GetDetailedPurchasesInputDto
    {
        public int ID { get; set; }
        public int PurchaseOrderID { get; set; }
    }
}