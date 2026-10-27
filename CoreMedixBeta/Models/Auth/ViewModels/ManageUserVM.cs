using System.Collections.Generic;

namespace CoreMedixBeta.Models.Auth.ViewModels
{
    public class ManageUserVM
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public bool IsNewUser { get; set; } = true;
        public List<PermissionGroupVM> PermissionGroups { get; set; } = new List<PermissionGroupVM>();
        public List<int> SelectedPermissionIDs { get; set; } = new List<int>();
    }

    public class PermissionGroupVM
    {
        public string Module { get; set; }
        public List<PermissionItemVM> Permissions { get; set; } = new List<PermissionItemVM>();
    }

    public class PermissionItemVM
    {
        public int ID { get; set; }
        public string PermissionKey { get; set; }
        public string PermissionName { get; set; }
        public bool IsSelected { get; set; }
    }
}
