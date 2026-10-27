using CoreMedixBeta.Models.Supplier;
using System;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports
{
    public class ExpiryReportVM : SaleReportVM
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; }

        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
    }
}