using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases.DTOs
{
    public class GetPagedPurchasesOutputDto
    {
        public int ID { get; set; }
        public int PurchaseOrderID { get; set; }
        public string ReferenceNo { get; set; }
        public string PurchaseDate { get; set; }
        public string SuplierName { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal SubTotalAfterTax { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}