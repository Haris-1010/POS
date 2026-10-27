using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedStockValuationReportInputDto : PagedRequestInputDto
    {
        public int CategoryID { get; set; }
        public int VarientID { get; set; }
        public int ItemID { get; set; }
        public int VariationID { get; set; }
        public int ManufacturerID { get; set; }
    }
}