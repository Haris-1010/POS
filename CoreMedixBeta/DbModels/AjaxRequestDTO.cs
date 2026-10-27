namespace CoreMedixBeta.DbModels
{
    public class AjaxRequestDTO
    {
        public string Message { get; set; } = "Unable to process your request due to technical error.";
        public bool Status { get; set; } = false;
        public dynamic Data { get; set; }
    }
}