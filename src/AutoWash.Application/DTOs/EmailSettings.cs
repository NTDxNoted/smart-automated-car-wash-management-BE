namespace AutoWash.Application.DTOs
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "AutoWash Pro";
        public string AppPassword { get; set; } = string.Empty;
    }
}
