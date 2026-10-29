using System.ComponentModel.DataAnnotations;

namespace CoreMedixBeta.Models.Backup.DTOs
{
    public class BackupFileActionInputDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "Invalid backup selection.")]
        public long HistoryId { get; set; }

        /// <summary>Required for restore: user must type RESTORE.</summary>
        public string ConfirmText { get; set; }
    }
}
