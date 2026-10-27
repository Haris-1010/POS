using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Discounts
{
    [Schema("Discount"), Table("Discounts")]
    public class Discount
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public string DiscountName { get; set; }
        public string DiscountType { get; set; } // Percentage or Value
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; }

        [DontUpdate]
        public DateTime TimeStamp { get; set; }
    }
}