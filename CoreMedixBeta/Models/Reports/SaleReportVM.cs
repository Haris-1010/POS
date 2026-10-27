using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports
{
    public class SaleReportVM
    {
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
        public List<GetManufacturerShortListOutputDto> Manufacturers { get; set; }
        public List<ManufacturerBrands> Brands { get; set; }
        public string CurrencySymbol { get; set; }
        public string EndDate { get; set; }
        public string StartDate { get; set; }
    }
}