using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems
{
    public class ItemsViewModel
    {
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
        public List<GetManufacturerShortListOutputDto> Manufacturers { get; set; } = new List<GetManufacturerShortListOutputDto>();

        public string FilterType { get; set; }
    }
}