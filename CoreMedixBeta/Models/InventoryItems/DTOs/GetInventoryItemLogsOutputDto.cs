using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetInventoryItemLogsOutputDto
    {
        public int ID { get; set; }
        public DateTime TimeStamp { get; set; }
        public string EventType { get; set; }
        public string Table { get; set; }
        public string Description { get; set; }
        public int AffectedID { get; set; }
        public string LogType { get; set; }
        public string VariationName { get; set; }
        public decimal Qty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Discount { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string IPAddress { get; set; }
    }
}