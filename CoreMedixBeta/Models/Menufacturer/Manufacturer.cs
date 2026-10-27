using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Menufacturer
{
    [Schema("Manufacturer")]
    [Table("Manufacturers")]
    public class Manufacturer
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 30;
        public const int MaxPhoneLength = 15;
        public const int MinPhoneLength = 11;

        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string City { get; set; }

        [DontUpdate]
        public string CreatedBy { get; set; }

        [DontUpdate]
        public DateTime CreatedOn { get; set; }

        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }

        [DontUpdate]
        public string DeletedBy { get; set; }

        [DontUpdate]
        public DateTime? DeletedOn { get; set; }

        [DontUpdate]
        public bool? IsDeleted { get; set; } = false;
    }
}