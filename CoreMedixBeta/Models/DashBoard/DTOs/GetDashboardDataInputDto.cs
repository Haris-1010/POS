using System;

namespace CoreMedixBeta.Models.DashBoard.DTOs
{
    public class GetDashboardDataInputDto
    {
        /// <summary>today | yesterday | thisweek | lastweek | thismonth | lastmonth | custom | alltime</summary>
        public string Range { get; set; }

        /// <summary>Only used when Range == "custom".</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Only used when Range == "custom".</summary>
        public DateTime? ToDate { get; set; }
    }
}
