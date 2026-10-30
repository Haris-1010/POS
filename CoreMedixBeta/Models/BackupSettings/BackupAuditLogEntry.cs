using InstaCareEMR.Core.DBHelpers;
using System;

namespace CoreMedixBeta.Models.BackupSettings
{
    [Schema("Settings")]
    [Table("BackupAuditLog")]
    public class BackupAuditLogEntry
    {
        [DontInsert]
        [DontUpdate]
        public int ID { get; set; }

        public string ActivityType { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
        public string PerformedBy { get; set; }
        public DateTime PerformedOn { get; set; }
    }
}
