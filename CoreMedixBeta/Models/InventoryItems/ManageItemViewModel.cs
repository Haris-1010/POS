using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems
{
    public class ManageItemViewModel
    {
        public List<GetAllAttributesOutputDto> AllAttributes { get; set; } = new List<GetAllAttributesOutputDto>();
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; } = new List<GetSupplierShortListOutputDto>();
        public List<GetManufacturerShortListOutputDto> Manufacturers { get; set; } = new List<GetManufacturerShortListOutputDto>();
        public List<ManufacturerBrands> ManufacturerBrands { get; set; } = new List<ManufacturerBrands>();
        public List<string> Cities { get; set; }
        public InventoryItem Item { get; set; }
        public int ItemID { get; set; } = 0;
    }
}