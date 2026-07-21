using AutoWash.Application.Common.Validation;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class LicensePlateValidatorTests
  {
    [Theory]
    [InlineData("30F-123.45")]
    [InlineData("51F12345")]
    [InlineData("29A-99999")]
    [InlineData("90A1-12345")]
    [InlineData("51f12345")] // chấp nhận chữ thường, tự uppercase trước khi so khớp
    public void IsValid_WithValidVietnamesePlates_ShouldReturnTrue(string plate)
    {
      Assert.True(LicensePlateValidator.IsValid(plate));
    }

    [Theory]
    [InlineData("AEDADAWDAWD")]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("ABCD-1234")] // không bắt đầu bằng 2 chữ số
    public void IsValid_WithInvalidInput_ShouldReturnFalse(string? plate)
    {
      Assert.False(LicensePlateValidator.IsValid(plate));
    }
  }
}
