namespace CoreMedixBeta.Models.Auth.DTOs
{
    public class LoginOutputDto
    {
        public bool IsValid { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }
}
