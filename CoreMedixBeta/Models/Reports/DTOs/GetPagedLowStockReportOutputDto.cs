using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedLowStockReportOutputDto
    {
        public int ItemID { get; set; }
        public string ProductName { get; set; }
        public string Varient { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Supplier { get; set; }
        public int LatestQty { get; set; }
        public int ReorderPoint { get; set; }
        public string Unit { get; set; }
    }
}