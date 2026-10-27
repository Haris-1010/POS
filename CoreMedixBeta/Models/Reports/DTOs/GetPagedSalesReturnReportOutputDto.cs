using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSalesReturnReportOutputDto
    {
        public string InvoiceNo { get; set; }
        public string DateTime { get; set; }
        public string Customer { get; set; }
        public string Product { get; set; }
        public int QtyReturned { get; set; }
        public decimal ReturnAmount { get; set; }
        public string User { get; set; }
    }
}