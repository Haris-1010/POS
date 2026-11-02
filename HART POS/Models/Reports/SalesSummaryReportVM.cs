using HARTPOS.Models.Discounts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using HARTPOS.Models.InventorySettings.DTOs;
using System.Web;

namespace HARTPOS.Models.Reports
{
    public class SalesSummaryReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }
        public List<GetDiscountsShortOutputDto> Discounts { get; set; } = new List<GetDiscountsShortOutputDto>();
    }
}