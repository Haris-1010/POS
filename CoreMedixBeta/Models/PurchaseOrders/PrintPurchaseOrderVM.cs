using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.PurchaseOrders.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders
{
    public class PrintPurchaseOrderVM : BasePrintVM
    {
        public GetDetailedPurchaseOrderOutputDto DetailedOrder { get; set; } = new GetDetailedPurchaseOrderOutputDto();
    }
}