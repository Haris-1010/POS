using CoreMedixBeta.Models.Patient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models
{
    public class PagedRequestInputDto
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string LoggedInUsername { get; set; }
        public string DoctorUsername { get; set; }
        public string Type { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsSecurityMode { get; set; } = false;
        public int SecurityRecords { get; set; } = 0;
        public string SortBy { get; set; } = string.Empty;
        public string SortByOrder { get; set; } = string.Empty;

        public DataTableValuesDTO DataTableValues { get; set; }
    }
}