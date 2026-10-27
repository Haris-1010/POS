using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.ReferralOrders.DTOs
{
    public class CreateOrderDto
    {
        public ReferralOrder Order { get; set; }
        public List<ReferralOrderItem> OrderItems { get; set; }
    }
}