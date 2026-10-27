using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedDeletedInvoicesReportOutputDto
    {
        public string InvoiceNo { get; set; }
        public string DeletedOn { get; set; }
        public string DeletedBy { get; set; }
        public decimal Amount { get; set; }
        public string Customer { get; set; }
    }
}