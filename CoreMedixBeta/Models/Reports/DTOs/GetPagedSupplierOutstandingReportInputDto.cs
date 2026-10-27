using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSupplierOutstandingReportInputDto : PagedRequestInputDto
    {
        public int SupplierID { get; set; }
        public int MinOutstandingAmount { get; set; }
    }
}