using HARTPOS.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Reports
{
    public class StockInHandReportVM : SaleReportVM
    {
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
    }
}