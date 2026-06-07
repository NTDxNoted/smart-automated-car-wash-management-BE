namespace AutoWash.Application.DTOs
{
  public class AuthResponse
  {
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
  }

  public class RegisterResponse
  {
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
  }

  public class ErrorResponse
  {
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
  }
}
