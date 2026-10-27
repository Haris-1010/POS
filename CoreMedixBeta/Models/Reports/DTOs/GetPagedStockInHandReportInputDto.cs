using CoreMedixBeta.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedStockInHandReportInputDto : PagedRequestInputDto
    {
        public int CategoryID { get; set; }
        public int ManufacturerID { get; set; }
        public int BrandID { get; set; }
        public string GroupByColumn { get; set; }
        public int SupplierID { get; set; }
        public QtyFilter QtyFilter { get; set; }
        public int QtyValue { get; set; }
    }
}