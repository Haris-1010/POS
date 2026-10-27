using System;
using System.Collections.Generic;
using System.Linq;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using System.Web;

namespace CoreMedixBeta.Models.Reports
{
    public class TopSellingProductsVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }
    }
}