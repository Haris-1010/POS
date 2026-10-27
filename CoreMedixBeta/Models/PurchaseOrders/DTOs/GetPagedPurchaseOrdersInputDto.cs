using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.PurchaseOrders.DTOs
{
    public class GetPagedPurchaseOrdersInputDto : PagedRequestInputDto
    {
        public string ReferenceNo { get; set; }
        public int SupplierID { get; set; }
        public string FilterType { get; set; }
        public string Status { get; set; }
    }
}