using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedDailySalesReportInputDto : PagedRequestInputDto
    {
        public int SupplierID { get; set; }
        public int ManufacturerID { get; set; }
        public int BrandID { get; set; }
        public string SalePersonUsername { get; set; }
        public string PaymentMode { get; set; }
    }
}