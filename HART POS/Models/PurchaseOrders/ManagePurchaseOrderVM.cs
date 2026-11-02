using HARTPOS.Models.InventoryItems.DTOs;
using HARTPOS.Models.PurchaseOrders.DTOs;
using HARTPOS.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.PurchaseOrders
{
    public class ManagePurchaseOrderVM
    {
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; }
        public List<GetManufacturerShortListOutputDto> Manufacturers { get; set; }
        public GetDetailedPurchaseOrderOutputDto DetailedOrder { get; set; } = new GetDetailedPurchaseOrderOutputDto();
        public int PurchaseOrderID { get; set; } = 0;
        public string ReferenceNo = string.Empty;
    }
}