using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedInventoryMovementReportOutputDto
    {
        public string ProductName { get; set; }
        public string TimeStamp { get; set; }
        public string TransactionType { get; set; }
        public int Qty { get; set; }
        public decimal Balance { get; set; }
        public string ReferenceNo { get; set; }
    }
}