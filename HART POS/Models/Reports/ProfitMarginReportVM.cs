using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HARTPOS.Models.InventorySettings.DTOs;

namespace HARTPOS.Models.Reports
{
    public class ProfitMarginReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}