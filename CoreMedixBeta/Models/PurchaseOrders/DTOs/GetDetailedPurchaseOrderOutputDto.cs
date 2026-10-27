using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class GetDetailedPurchaseOrderOutputDto
    {
        public PurchaseOrderDto Order { get; set; } = new PurchaseOrderDto();
        public List<PurchaseOrderItem> OrderItems { get; set; } = new List<PurchaseOrderItem>();
    }

    public class PurchaseOrderDto
    {
        public int ID { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string SupplierPhone { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderDateFormatted { get; set; }
        public string Status { get; set; }
        public string BusinessLocation { get; set; } = string.Empty;
        public string BusinessLocationName { get; set; }
        public decimal SubTotal { get; set; }
        public string LocationID { get; set; }
        public string MasterUsername { get; set; }
        public bool? IsPurchaseReceived { get; set; } = false;
    }
}