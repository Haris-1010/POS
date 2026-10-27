using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedSlowItemsReportOutputDto
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int QtySold { get; set; }
        public string DateTime { get; set; }
        public int OpeningStock { get; set; }
    }
}