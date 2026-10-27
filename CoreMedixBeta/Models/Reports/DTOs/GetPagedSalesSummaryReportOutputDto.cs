using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSalesSummaryReportOutputDto
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Qty { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal SaleRate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Discount { get; set; }
        public string DiscountType { get; set; }
        public decimal NetSale { get; set; }
        public decimal Profit { get; set; }
    }
}