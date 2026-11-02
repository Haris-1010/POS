using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Reports.DTOs
{
    public class GetPagedDailyActivityReportInputDto : PagedRequestInputDto
    {
        public string SalePersonUsername { get; set; }
    }
}