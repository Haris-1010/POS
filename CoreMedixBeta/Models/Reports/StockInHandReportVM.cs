using CoreMedixBeta.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports
{
    public class StockInHandReportVM : SaleReportVM
    {
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
    }
}