using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedIncomReportOutputDto
    {
        public string InvoiceNo { get; set; }
        public string DateTime { get; set; }
        public decimal NetAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalIncome { get; set; }
        public string PaymentMode { get; set; }
        public string Customer { get; set; }
    }
}