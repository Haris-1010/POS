using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedManualAdjustmentReportInputDto : PagedRequestInputDto
    {
        public string SalePersonUsername { get; set; }
        public int ItemID { get; set; }
        public int VariationID { get; set; }
    }
}