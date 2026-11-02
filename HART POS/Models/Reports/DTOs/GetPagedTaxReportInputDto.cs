using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Reports.DTOs
{
    public class GetPagedTaxReportInputDto : PagedRequestInputDto
    {
        public string SalePersonUsername { get; set; }
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }
    }
}