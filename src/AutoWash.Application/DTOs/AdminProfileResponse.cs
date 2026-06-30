using System;

namespace AutoWash.Application.DTOs
{
    public class AdminProfileResponse
    {
        public int AdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
        public DateTime CreatedAt { get; set; }
    }
}
