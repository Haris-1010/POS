using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class GetSaleInvoiceInputDto
    {
        public int InvoiceID { get; set; }
        public bool IsPrint { get; set; } = false;
    }
}