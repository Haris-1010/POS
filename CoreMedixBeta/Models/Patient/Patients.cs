using CoreMedixBeta.Models.Patient;
using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Patient
{
    [Schema("Patient"), Table("Patients")]
    public class Patients : PagedRequest
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 30;
        public const int MaxPhoneLength = 15;
        public const int MinPhoneLength = 11;

        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public int PatientTypeID { get; set; }

        public string Name { get; set; }
        public string Phone { get; set; }
        public string CNIC { get; set; }
        public string City { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public string DOB { get; set; }  // یا DateTime? بنائیں اگر possible ہو

        public string Gender { get; set; }

        public string Address { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        public string LastModifiedBy { get; set; }

        [DontUpdate]
        public string MR_No { get; set; }

        public DateTime? LastModifiedOn { get; set; }

        [DontUpdate]
        public string DeletedBy { get; set; }

        [DontUpdate]
        public DateTime? DeletedOn { get; set; }

        [DontUpdate]
        public bool IsDeleted { get; set; } = false;
    }
}