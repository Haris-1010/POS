using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HARTPOS.Models.Purchases.DTOs
{
    public class GetPagedPurchasesInputDto : PagedRequestInputDto
    {
        public string ReferenceNo { get; set; }
        public int SupplierID { get; set; }
        public string Status { get; set; }
        public string FilterType { get; set; }
    }
}