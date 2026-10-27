using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedUserActivityReportOutputDto
    {
        public string DateTime { get; set; }
        public string User { get; set; }
        public string ActionType { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }
    }
}