using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedTopSellingProductsOutputDto
    {
        public string ProductName { get; set; }
        public string Varient { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public int QtySold { get; set; }
        public decimal TotalSale { get; set; }
        public int SaleCount { get; set; }
    }
}