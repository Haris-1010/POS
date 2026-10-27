using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Brandings
{
    [Schema("Brandings"), Table("PrintBrandingCustomFields")]
    public class PrintBrandingCustomField
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string ForModule { get; set; }
        public bool? InVisible { get; set; }
        public int Order { get; set; }
        public int BrandingId { get; set; }
    }
}