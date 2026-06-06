using System;

namespace AutoWash.Application.Exceptions
{
  public class AuthException : Exception
  {
    public string ErrorCode { get; }

    public AuthException(string errorCode, string message) : base(message)
    {
      ErrorCode = errorCode;
    }
  }
}
