using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Patient.DTOs
{
    public class SearchPatientOutputDto
    {
        public string CustomerName { get; set; }
        public string CustomerType { get; set; }
        public string CustomerID { get; set; }
        public string Phone { get; set; }
    }
}