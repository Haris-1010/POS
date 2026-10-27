using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSaleReportOutputDto
    {
        public decimal LineTotal { get; set; }
        public decimal RatePerUnit { get; set; }
        public int Qty { get; set; }
        public string ItemName { get; set; }
        public string DateTime { get; set; }
        public string CustomerName { get; set; } 
        public string ManufacturerName { get; set; }
        public string MrNumber { get; set; }
    }
}