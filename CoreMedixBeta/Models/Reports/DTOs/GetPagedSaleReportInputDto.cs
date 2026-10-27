using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSaleReportInputDto : PagedRequestInputDto
    {
        public int SupplierID { get; set; }
        public int ManufacturerID { get; set; }
        public string ReferredByID { get; set; }
        public string GroupByColumn { get; set; }
        public string ReferredByType { get; set; }
        public int BrandID { get; set; }
        public string[] Items { get; set; }
    }
}