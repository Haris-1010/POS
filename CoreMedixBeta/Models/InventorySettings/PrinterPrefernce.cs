using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.InventorySettings
{
    [Schema("Printer")]
    [Table("PrinterPrefernces")]
    public class PrinterPrefernce
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string Type { get; set; }

        [DontInsert]
        public string LastModifiedBy { get; set; }

        [DontInsert]
        public DateTime? LastModifiedOn { get; set; }
    }
}