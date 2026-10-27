using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedManufacturerWiseSalesReportInputDto : PagedRequestInputDto
    {
        public int ManufacturerID { get; set; }
        public string GroupByColumn { get; set; }
    }
}