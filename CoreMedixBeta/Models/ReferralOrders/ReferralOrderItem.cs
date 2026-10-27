using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.ReferralOrders
{
    [Schema("ReferralOrder"), Table("ReferralOrderItems")]
    public class ReferralOrderItem
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        [DontUpdate]
        public int OrderID { get; set; }

        [DontUpdate]
        public int PrescriptionMedicineID { get; set; }

        [DontUpdate]
        public int ItemID { get; set; }

        [DontUpdate]
        public int VariationID { get; set; }
    }
}