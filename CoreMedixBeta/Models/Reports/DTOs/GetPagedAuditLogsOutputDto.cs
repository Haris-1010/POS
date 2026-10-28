namespace CoreMedixBeta.Models.Reports.DTOs
{
    public class GetPagedAuditLogsOutputDto
    {
        public long ID { get; set; }
        public string CreatedOn { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityID { get; set; }
        public string Details { get; set; }
        public string IPAddress { get; set; }
    }
}
