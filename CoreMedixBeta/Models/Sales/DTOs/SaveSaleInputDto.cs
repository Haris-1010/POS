using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class SaveSaleInputDto
    {
        public SaleInvoice SaleInvoice { get; set; }
        public SaleInvoiceFinancialDetail SaleInvoiceFinancialDetails { get; set; }
        public List<SaleInvoiceItem> SaleInvoiceItems { get; set; }
        public List<SaleInvoicePayment> SaleInvoicePayments { get; set; }
        public string CurrentUsername { get; set; }
    }
}