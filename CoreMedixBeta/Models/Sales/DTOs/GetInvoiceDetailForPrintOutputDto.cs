using CoreMedixBeta.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales.DTOs
{
    public class GetInvoiceDetailForPrintOutputDto
    {
        public SaleInvoiceDto Invoice { get; set; }
        public List<SaleInvoiceItemsDto> InvoiceItems { get; set; }
        public SaleInvoiceCustomerDto Customer { get; set; }
        public SaleInvoiceFinancialsDto InvoiceFinancials { get; set; }
        public List<SaleInvoicePaymentsDto> InvoicePayments { get; set; }
    }

    public class SaleInvoiceDto
    {
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; } = ServerConfig.DateTimeNow;
        public string Remarks { get; set; }
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }
        public string CustomerName { get; set; }
        public string SalePersonName { get; set; }
        public string SalePersonUsername { get; set; }
        public string ReferredByID { get; set; }
        public string ReferredByName { get; set; }
        public string ReferredByType { get; set; }
        public string LocationID { get; set; }
        public string MasterUsername { get; set; }
        public string InvoiceDateFormated { get; set; }
    }

    public class SaleInvoiceItemsDto
    {
        public int VariationID { get; set; }
        public int ItemID { get; set; }
        public string Name { get; set; }
        public string Qty { get; set; }
        public decimal Discount { get; set; }
        public decimal UnitCost { get; set; }
        public string DiscountType { get; set; }
        public decimal LineTotal { get; set; }
        public string Weight { get; set; }
        public string WeightUnit { get; set; }
        public string Formation { get; set; }
    }

    public class SaleInvoiceCustomerDto
    {
        public string Name { get; set; }
        public string Phone { get; set; }
    }

    public class SaleInvoiceFinancialsDto
    {
        public decimal SubTotal { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal SubtotalAfterDiscount { get; set; }
        public int Tax { get; set; }
        public string TaxTitle { get; set; }
        public decimal SubtotalAfterTax { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public string AdditionalNotes { get; set; }
    }

    public class SaleInvoicePaymentsDto
    {
        public string Date { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMode { get; set; }
        public string AttachmentURL { get; set; }
    }
}