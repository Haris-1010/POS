using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class GetPagedPurchaseOrdersOutputDto
    {
        public int ID { get; set; }
        public int PurchaseID { get; set; }
        public string ReferenceNo { get; set; }
        public string Date { get; set; }
        public string SuplierName { get; set; }
        public string SuplierPhone { get; set; }
        public string Status { get; set; }
        public int TotalItems { get; set; }
        public decimal SubTotal { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsPurchaseReceived { get; set; }
    }
}