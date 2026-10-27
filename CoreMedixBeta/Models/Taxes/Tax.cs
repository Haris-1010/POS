using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Taxes
{
    [Schema("Tax")]
    [Table("Taxes")]
    public class Tax
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 30;
        public const int MaxPhoneLength = 15;
        public const int MinPhoneLength = 11;

        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string Title { get; set; }
        public decimal Percentage { get; set; }
        public string PaymentMode { get; set; }

        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
    }
}