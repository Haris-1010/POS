using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedPurchaseReportInputDto : PagedRequestInputDto
    {
        public string Status { get; set; }
        public int ItemID { get; set; }
        public int VariationID { get; set; }
        public int SupplierID { get; set; }
        public int ManufacturerID { get; set; }
        public int BrandID { get; set; }
    }
}