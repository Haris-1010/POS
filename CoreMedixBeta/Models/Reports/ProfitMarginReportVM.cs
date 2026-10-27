using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CoreMedixBeta.Models.InventorySettings.DTOs;

namespace CoreMedixBeta.Models.Reports
{
    public class ProfitMarginReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}