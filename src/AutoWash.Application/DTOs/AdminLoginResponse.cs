namespace AutoWash.Application.DTOs
{
    public class AdminLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int AdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
    }
}
