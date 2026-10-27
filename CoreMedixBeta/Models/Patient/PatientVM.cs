using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Patient
{
    public class PatientVM
    {
        public List<string> Cities { get; set; }
        public Patients Patient { get; set; }
    }
}