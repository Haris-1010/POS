using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedDailyActivityReportOutputDto
    {
        public string User { get; set; }
        public int TotalSales { get; set; }
        public int TotalReturns { get; set; }
        public int TotalPurchases { get; set; }
        public int UserActivityCount { get; set; }
    }
}