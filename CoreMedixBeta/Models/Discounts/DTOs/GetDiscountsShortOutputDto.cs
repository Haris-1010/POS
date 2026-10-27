using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Discounts.DTOs
{
    public class GetDiscountsShortOutputDto
    {
        public int ID { get; set; }
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; }
        public string DiscountType { get; set; }
        public string DiscountName { get; set; }
    }
}