using CoreMedixBeta.Models.PurchaseOrders.DTOs;
using CoreMedixBeta.Models.Purchases.DTOs;
using CoreMedixBeta.Models.Taxes.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders
{
    public class ManagePurchaseVM
    {
        public GetDetailedPurchasesOutputDto Purchase { get; set; } = new GetDetailedPurchasesOutputDto();
        public GetDetailedPurchaseOrderOutputDto DetailedOrder { get; set; } = new GetDetailedPurchaseOrderOutputDto();
        public List<TaxesShortListOutputDto> Taxes { get; set; } = new List<TaxesShortListOutputDto>();
        public int PurchaseID { get; set; } = 0;
        public int PurchaseOrderID { get; set; } = 0;
    }
}