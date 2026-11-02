using HARTPOS.Models.InventoryItems.DTOs;
using HARTPOS.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.InventoryItems
{
    public class ItemsViewModel
    {
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
        public List<GetManufacturerShortListOutputDto> Manufacturers { get; set; } = new List<GetManufacturerShortListOutputDto>();

        public string FilterType { get; set; }
    }
}