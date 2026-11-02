using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.DashBoard.DTOs
{
    public class SaveDashboardSettingInputDto
    {
        public List<DashboardSetting> DashboardSettings { get; set; }
    }
}