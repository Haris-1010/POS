using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedDailySalesReportOutputDto
    {
        public string InvoiceNo { get; set; }
        public string DateTime { get; set; }
        public string CustomerName { get; set; }
        public int ItemsCount { get; set; }
        public int TotalQty { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
    }
}