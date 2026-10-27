using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedCustomerLedgerOutputDto
    {
        public string DateTime { get; set; }
        public string InvoiceNo { get; set; }
        public string Description { get; set; }
        public string CustomerName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }
}