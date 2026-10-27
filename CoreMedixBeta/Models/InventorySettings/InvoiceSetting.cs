using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventorySettings
{
    [Schema("Invoice")]
    [Table("InvoiceSettings")]
    public class InvoiceSetting
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string InvoicePrefix { get; set; }
        public string Currency { get; set; }
        public string UserName { get; set; }
    }
}