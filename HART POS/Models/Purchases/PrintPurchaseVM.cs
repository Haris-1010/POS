using HARTPOS.DbModels;
using HARTPOS.Models.Purchases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Purchases
{
    public class PrintPurchaseVM : BasePrintVM
    {
        public GetDetailedPurchasesOutputDto Purchase { get; set; }
    }
}