using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedBatchWiseStockReportInputDto : PagedRequestInputDto
    {
        public int CategoryID { get; set; }
        public int VarientID { get; set; }
        public int ManufacturerID { get; set; }
        public string GroupByColumn { get; set; }
        public int SupplierID { get; set; }
        public int ItemID { get; set; }
    }
}