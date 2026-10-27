using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Purchases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases
{
    public class PrintPurchaseVM : BasePrintVM
    {
        public GetDetailedPurchasesOutputDto Purchase { get; set; }
    }
}