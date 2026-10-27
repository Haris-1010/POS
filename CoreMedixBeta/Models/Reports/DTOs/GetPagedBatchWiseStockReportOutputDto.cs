using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedBatchWiseStockReportOutputDto
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string Variant { get; set; }
        public string Batch { get; set; }
        public string ExpiryDate { get; set; }
        public decimal OpeningStock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
    }
}