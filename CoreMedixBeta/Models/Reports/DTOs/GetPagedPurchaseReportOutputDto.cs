using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedPurchaseReportOutputDto
    {
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
        public string OrderDate { get; set; }
        public string PurchaseReceivedAt { get; set; }
        public string Status { get; set; }
        public string Supplier { get; set; }
        public string ProductName { get; set; }
        public string Batch { get; set; }
        public int Qty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}