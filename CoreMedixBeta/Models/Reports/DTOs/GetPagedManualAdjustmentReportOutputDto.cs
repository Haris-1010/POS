using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedManualAdjustmentReportOutputDto
    {
        public string DateTime { get; set; }
        public string ProductName { get; set; }
        public string AdjustmentType { get; set; }
        public int Qty { get; set; } 
    }
}