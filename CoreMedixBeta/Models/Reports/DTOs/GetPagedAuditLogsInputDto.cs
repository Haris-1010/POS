namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedAuditLogsInputDto : PagedRequestInputDto
    {
        public string Action { get; set; }
        public string Username { get; set; }
    }
}
