using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models
{
    public class AjaxRequestInputDTO
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string DoctorUsername { get; set; } = string.Empty;
        public string LocationID { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string SortBy { get; set; } = string.Empty;
        public string SortByOrder { get; set; } = string.Empty;
        public string SearchString { get; set; } = string.Empty;
    }
}