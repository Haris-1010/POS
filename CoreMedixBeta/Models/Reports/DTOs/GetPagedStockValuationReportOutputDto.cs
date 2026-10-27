using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedStockValuationReportOutputDto
    {
        public string VariationName { get; set; }
        public string Variant { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public int OpeningStock { get; set; }
        public int CostPrice { get; set; }
        public decimal TotalValue { get; set; }
    }
}