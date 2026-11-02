using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Reports.DTOs
{
    public class GetPagedIncomReportInputDto : PagedRequestInputDto
    {
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }
        public string SalePersonUsername { get; set; }
    }
}