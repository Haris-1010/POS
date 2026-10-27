using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    public class SaleInvoicesVM
    {
        public string CurrencySymbol { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CustomerId { get; set; }
    }
}