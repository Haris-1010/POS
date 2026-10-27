using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedManufacturerWiseSalesReportOutputDto
    {
        public string Manufacturer { get; set; }
        public string ProductName { get; set; }
        public int Qty { get; set; }
        public decimal NetSale { get; set; }
    }
}