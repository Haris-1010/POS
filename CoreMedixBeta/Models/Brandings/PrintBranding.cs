using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Brandings
{
    [Schema("Brandings"), Table("PrintBrandings")]
    public class PrintBranding
    {
        [DontInsert, DontUpdate]
        public int ID { get; set; }

        public string BusinessName { get; set; }
        public string BusinessPhone { get; set; }
        public string BusinessEmail { get; set; }
        public string Logo { get; set; }
        public string HeaderImg { get; set; }
        public string HeaderText { get; set; }
        public string FooterImg { get; set; }
        public string FooterText { get; set; }

        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedOn { get; set; }
    }
}