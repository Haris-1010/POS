using System.Collections.Generic;

namespace CoreMedixBeta.Models.Auth.DTOs
{
    public class ManageUserInputDto
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public List<int> PermissionIDs { get; set; } = new List<int>();
    }
}
