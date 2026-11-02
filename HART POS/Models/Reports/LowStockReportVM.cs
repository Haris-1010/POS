using HARTPOS.Models.Supplier;
using System;
using HARTPOS.Models.InventorySettings.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Reports
{
    public class LowStockReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }

        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
    }
}