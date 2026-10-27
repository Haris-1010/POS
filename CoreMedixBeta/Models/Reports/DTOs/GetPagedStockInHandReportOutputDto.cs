using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedStockInHandReportOutputDto
    {
        public string ItemName { get; set; } 
        public string Supplier { get; set; }
        public string Manufacturer { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int SKU { get; set; }
        public decimal PurchasePrice { get; set; }
        public int Qty { get; set; }
        public string Unit { get; set; }
        public string PackUnits { get; set; }
        public string Category { get; set; }
        public string Value { get; set; }
    }
}