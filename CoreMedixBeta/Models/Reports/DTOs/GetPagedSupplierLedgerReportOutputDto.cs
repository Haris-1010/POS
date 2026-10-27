using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSupplierLedgerReportOutputDto
    {
        public string ReferenceNo { get; set; }
        public string DateTime { get; set; }
        public string Status { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public string SupplierName { get; set; }
        public string Description { get; set; }
    }
}