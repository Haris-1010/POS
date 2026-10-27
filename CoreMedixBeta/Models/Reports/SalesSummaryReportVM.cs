using CoreMedixBeta.Models.Discounts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using System.Web;

namespace CoreMedixBeta.Models.Reports
{
    public class SalesSummaryReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }
        public List<GetDiscountsShortOutputDto> Discounts { get; set; } = new List<GetDiscountsShortOutputDto>();
    }
}