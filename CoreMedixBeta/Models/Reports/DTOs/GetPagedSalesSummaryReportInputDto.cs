using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSalesSummaryReportInputDto : PagedRequestInputDto
    {
        public string SalePersonUsername { get; set; }
        public int DiscountID { get; set; }
        public int CategoryID { get; set; }
        public int SupplierID { get; set; }
        public int ManufacturerID { get; set; }
    }
}