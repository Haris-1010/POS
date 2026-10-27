using InstaCareEMR.Core.DBHelpers;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.Auth
{
    [Schema("Auth"), Table("Users")]
    public class Users
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string Username { get; set; }

        [DontInsert]
        [DontUpdate]
        public string PasswordHash { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        [DontInsert]
        public DateTime CreatedOn { get; set; }

        [DontInsert]
        public string CreatedBy { get; set; }

        public DateTime? LastModifiedOn { get; set; }

        public string LastModifiedBy { get; set; }

        [DontInsert]
        [DontUpdate]
        public bool IsDeleted { get; set; } = false;

        [DontInsert]
        [DontUpdate]
        public DateTime? DeletedOn { get; set; }

        [DontInsert]
        [DontUpdate]
        public string DeletedBy { get; set; }
    }
}
