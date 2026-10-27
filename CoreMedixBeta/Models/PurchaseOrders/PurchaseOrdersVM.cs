using CoreMedixBeta.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders
{
    public class PurchaseOrdersVM
    {
        public List<GetSupplierShortListOutputDto> SuppliersShortList { get; set; }
        public string CurrencySymbol { get; set; }
    }
}