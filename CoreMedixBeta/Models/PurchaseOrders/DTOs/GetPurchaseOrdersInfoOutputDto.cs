using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class GetPurchaseOrdersInfoOutputDto
    {
        public int TotalOrders { get; set; }
        public int TotalReceived { get; set; }
        public int TotalPending { get; set; }
        public int AllOrders { get; set; }
    }
}