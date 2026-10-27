using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedCustomerOutstandingReportOutputDto
    {
        public string Customer { get; set; }
        public string DateTime { get; set; }
        public decimal TotalSale { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
    }
}