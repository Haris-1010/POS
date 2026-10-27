using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class GetSaleInvoiceOutputDto
    {
        public SaleInvoiceDto SaleInvoice { get; set; } = new SaleInvoiceDto();
        public List<SaleInvoiceItem> SaleInvoiceItems { get; set; } = new List<SaleInvoiceItem>();
        public SaleInvoiceFinancialDetail SaleInvoiceFinancialDetails { get; set; } = new SaleInvoiceFinancialDetail();
        public List<SaleInvoicePayment> SaleInvoicePayments { get; set; } = new List<SaleInvoicePayment>();
    }
}