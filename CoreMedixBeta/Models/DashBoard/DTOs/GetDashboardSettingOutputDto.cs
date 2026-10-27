using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.DashBoard.DTOs
{
    public class GetDashboardSettingOutputDto
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public bool? IsVisible { get; set; }
        public int CardID { get; set; }
        public string UserID { get; set; }
    }
}