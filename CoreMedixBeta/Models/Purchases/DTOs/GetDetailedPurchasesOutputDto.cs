using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Purchases.DTOs
{
    public class GetDetailedPurchasesOutputDto
    {
        public PurchseDetailsDto Purchase { get; set; } = new PurchseDetailsDto();
        public List<PurchaseItemsDto> PurchaseItems { get; set; } = new List<PurchaseItemsDto>();
        public List<PurchasePaymentsDto> PurcahsePayments { get; set; } = new List<PurchasePaymentsDto>();
    }

    public class PurchasePaymentsDto
    {
        public int ID { get; set; }
        public int PurchaseID { get; set; }
        public string DateFormated { get; set; }
        public DateTime Date { get; set; }
        public string PaymentMode { get; set; }
        public string AttachmentURL { get; set; }
        public decimal PaidAmount { get; set; }
    }

    public class PurchaseItemsDto
    {
        public int PurchaseID { get; set; }
        public string Name { get; set; }
        public string Formation { get; set; }
        public decimal Weight { get; set; }
        public string WeightUnit { get; set; }
        public int Qty { get; set; }
        public decimal ItemTax { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }
        public int ID { get; set; }
        public int ItemID { get; set; }
        public int VariationID { get; set; }
        public int BonusQty { get; set; }
        public decimal UnitCostWithTax { get; set; }
    }

    public class PurchseDetailsDto
    {
        public int ID { get; set; }
        public int PurchaseOrderID { get; set; }
        public DateTime PurchaseReceivedAt { get; set; }
        public string PurchaseReceivedAtFormated { get; set; }
        public double SubTotal { get; set; }
        public string DiscountType { get; set; }
        public double DiscountAmount { get; set; }
        public double SubTotalAfterDiscount { get; set; }
        public int TaxID { get; set; }
        public double SubtotalAfterTax { get; set; }
        public double PaidAmount { get; set; }
        public double RemainingAmount { get; set; }
        public string ShippingDetails { get; set; }
        public string AdditionalNotes { get; set; }
        public string SupplierName { get; set; }
        public int SupplierID { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierCity { get; set; }
        public string ReferenceNo { get; set; }
        public string Status { get; set; }
        public string LocationName { get; set; }
        public double Tax { get; set; }
        public string TaxTitle { get; set; }
    }
}