using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.DashBoard
{
    [Schema("Dashboard"), Table("DashboardSettings")]
    public class DashboardSetting
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public string Title { get; set; }
        public int Order { get; set; }
        public bool? IsVisible { get; set; }
        public int CardID { get; set; }
        public string UserID { get; set; }
    }
}