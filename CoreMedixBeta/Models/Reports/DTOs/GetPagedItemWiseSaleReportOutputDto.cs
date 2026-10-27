using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedItemWiseSaleReportOutputDto
    {
        public string ItemName { get; set; }
        public string DoctorName { get; set; }
        public string DateTime { get; set; }
        public int TotalCustomers { get; set; }
        public int Qty { get; set; }
        public decimal PerUnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}