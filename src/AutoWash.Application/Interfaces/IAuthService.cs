using AutoWash.Application.DTOs;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
  public interface IAuthService
  {
    /// <summary>
    /// Registers a new customer with phone and password
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Registered customer details</returns>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates customer with phone and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Validates JWT token and returns customer claims
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Token claims</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets customer ID from JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Customer ID</returns>
    int? GetCustomerIdFromToken(string token);
  }
}
