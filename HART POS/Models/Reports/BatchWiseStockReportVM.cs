using System;
using System.Collections.Generic;
using System.Linq;
using HARTPOS.Models.InventorySettings.DTOs;
using System.Web;

namespace HARTPOS.Models.Reports
{
    public class BatchWiseStockReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }
    }
}