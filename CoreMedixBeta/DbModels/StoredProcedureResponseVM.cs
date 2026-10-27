using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public class StoredProcedureResponseVM
    {
        public bool Status { get; set; } = false;
        public string Message { get; set; }
        public int ID { get; set; } = 0;
    }
}