using InstaCareEMR.Core.DBHelpers;

namespace CoreMedixBeta.Models.Auth
{
    [Schema("Auth"), Table("Permissions")]
    public class Permissions
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string PermissionKey { get; set; }

        public string PermissionName { get; set; }

        public string Module { get; set; }
    }
}
