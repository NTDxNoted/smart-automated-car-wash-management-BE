using AutoWash.Application.DTOs;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
  public interface IAuthService
  {
    /// <summary>
    /// Registers a new customer with phone, email and password. Sends a registration OTP to the email.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Registered customer details</returns>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates customer with phone and password. Throws TwoFactorRequiredException
    /// when the account has 2FA enabled instead of returning a token directly.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Completes a 2FA login by verifying the OTP and issuing the JWT.
    /// </summary>
    Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request);

    /// <summary>
    /// Verifies the OTP sent at registration and marks the customer's email as verified.
    /// </summary>
    Task VerifyEmailAsync(VerifyEmailRequest request);

    /// <summary>
    /// Resends the registration verification OTP.
    /// </summary>
    Task ResendVerificationEmailAsync(ResendOtpRequest request);

    /// <summary>
    /// Sends a password-reset OTP if the email belongs to a customer (silent no-op otherwise).
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Verifies the password-reset OTP and updates the password.
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Enables or disables 2FA for the given customer.
    /// </summary>
    Task SetTwoFactorEnabledAsync(int customerId, bool enable);

    /// <summary>
    /// Validates JWT token and returns customer claims
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Token claims</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Logs out the current customer session by clearing the active session id.
    /// </summary>
    Task LogoutAsync(int customerId);

    /// <summary>
    /// Gets customer ID from JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Customer ID</returns>
    int? GetCustomerIdFromToken(string token);
  }
}
