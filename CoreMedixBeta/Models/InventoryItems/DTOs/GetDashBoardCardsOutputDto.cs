using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetDashBoardCardsOutputDto
    {
        public decimal TotalProfitToday { get; set; }
        public decimal TotalSaleToday { get; set; }
        public int TotalInvoicesToday { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalStockValue { get; set; }
        public int TodayStockSoldValue { get; set; }
        public int TotalExpiryItems { get; set; }
        public int TotalExpirySoonItems { get; set; }
        public int TotalStock { get; set; }
        public int TotalItems { get; set; }
        public int LowStockItems { get; set; }
        public int NewArrivalItems { get; set; }
    }
}