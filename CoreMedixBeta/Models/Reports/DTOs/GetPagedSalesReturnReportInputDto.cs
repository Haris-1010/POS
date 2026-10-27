using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSalesReturnReportInputDto : PagedRequestInputDto
    {
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }
        public int ItemID { get; set; }
        public int VariationID { get; set; }
        public string SalePersonUsername { get; set; }
        public int SupplierID { get; set; }
        public int ManufacturerID { get; set; }
        public int BrandID { get; set; }
    }
}