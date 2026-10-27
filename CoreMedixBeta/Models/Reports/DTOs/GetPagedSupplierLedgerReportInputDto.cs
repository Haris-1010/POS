using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSupplierLedgerReportInputDto : PagedRequestInputDto
    {
        public string Status { get; set; }
        public int SupplierID { get; set; }
    }
}