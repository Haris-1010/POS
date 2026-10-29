namespace CoreMedixBeta.Models.BackupSettings.DTOs
{
    public class BrowseFolderInputDto
    {
        /// <summary>Empty/null means "list drives" (the browser's starting point).</summary>
        public string Path { get; set; }
    }
}
