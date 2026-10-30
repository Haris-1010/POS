using System;

namespace CoreMedixBeta.Models.BackupSettings
{
    public class BackupSettingsVM
    {
        public BackupSetting Settings { get; set; } = new BackupSetting();

        public string StatusText { get; set; } = "Disabled";
        public string StatusBadgeClass { get; set; } = "badge-secondary";

        public string LastBackupTimeFormatted { get; set; } = "Never";
        public string NextBackupTimeFormatted { get; set; } = "Not scheduled";

        public string LastBackupFileName { get; set; } = "None";
        public string LastBackupFileSize { get; set; } = "-";
        public DateTime? LastSuccessTime { get; set; }

        public bool FolderExists { get; set; }
        public bool FolderWritable { get; set; }
        public string FolderError { get; set; }
        public string FolderFreeSpace { get; set; }

        public string DatabaseName { get; set; }
        public bool IsBackupRunning { get; set; }
        public string LastErrorMessage { get; set; }
    }
}
