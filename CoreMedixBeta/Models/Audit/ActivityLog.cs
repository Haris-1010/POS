using InstaCareEMR.Core.DBHelpers;
using System;

namespace CoreMedixBeta.Models.Audit
{
    [Schema("dbo")]
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        [DontInsert]
        [DontUpdate]
        public long ID { get; set; }

        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityID { get; set; }
        public string Details { get; set; }
        public string IPAddress { get; set; }

        [DontInsert]
        [DontUpdate]
        public DateTime CreatedOn { get; set; }
    }
}
