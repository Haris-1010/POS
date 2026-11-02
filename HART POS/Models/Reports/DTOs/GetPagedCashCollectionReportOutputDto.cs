using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Reports.DTOs
{
    public class GetPagedCashCollectionReportOutputDto
    {
        public string DateTime { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } 
    }
}