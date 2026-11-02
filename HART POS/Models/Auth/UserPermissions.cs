using InstaCareEMR.Core.DBHelpers;

namespace HARTPOS.Models.Auth
{
    [Schema("Auth"), Table("UserPermissions")]
    public class UserPermissions
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public int UserID { get; set; }

        public int PermissionID { get; set; }
    }
}
