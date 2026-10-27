using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedProfitMarginReportOutputDto
    {
        public string ProductName { get; set; }
        public string Variant { get; set; }
        public string Category { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal SaleRate { get; set; }
        public int QtySold { get; set; }
        public decimal ProfitPerUnit { get; set; }
        public decimal TotalProfit { get; set; }
    }
}