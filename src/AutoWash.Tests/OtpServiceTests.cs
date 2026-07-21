using System.Collections.Concurrent;
using System.Reflection;
using AutoWash.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class OtpServiceTests
  {
    private static OtpService CreateService() => new OtpService(Mock.Of<ILogger<OtpService>>());

    // OtpService lưu OTP trong ConcurrentDictionary private, cần reflection để test nhánh hết hạn
    // vì không có cách nào chờ thật 5 phút hoặc inject đồng hồ giả trong thiết kế hiện tại.
    private static void ForceExpire(OtpService service, string phone)
    {
      var field = typeof(OtpService).GetField("_store", BindingFlags.NonPublic | BindingFlags.Instance)!;
      var store = (ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)>)field.GetValue(service)!;
      var entry = store[phone];
      store[phone] = (entry.Code, DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void GenerateAndStore_ShouldReturnSixDigitNumericCode()
    {
      var service = CreateService();

      var code = service.GenerateAndStore("0901234567");

      Assert.Equal(6, code.Length);
      Assert.True(int.TryParse(code, out _));
    }

    [Fact]
    public void Verify_WithCorrectCode_ShouldReturnTrue()
    {
      var service = CreateService();
      var code = service.GenerateAndStore("0901234567");

      var isValid = service.Verify("0901234567", code);

      Assert.True(isValid);
    }

    [Fact]
    public void Verify_WithCorrectCode_ShouldConsumeCodeSoSecondVerifyFails()
    {
      var service = CreateService();
      var code = service.GenerateAndStore("0901234567");

      Assert.True(service.Verify("0901234567", code));
      Assert.False(service.Verify("0901234567", code));
    }

    [Fact]
    public void Verify_WithWrongCode_ShouldReturnFalse()
    {
      var service = CreateService();
      service.GenerateAndStore("0901234567");

      var isValid = service.Verify("0901234567", "000000");

      Assert.False(isValid);
    }

    [Fact]
    public void Verify_WithNoStoredCodeForPhone_ShouldReturnFalse()
    {
      var service = CreateService();

      var isValid = service.Verify("0900000000", "123456");

      Assert.False(isValid);
    }

    [Fact]
    public void Verify_WithExpiredCode_ShouldReturnFalse()
    {
      var service = CreateService();
      var code = service.GenerateAndStore("0901234567");
      ForceExpire(service, "0901234567");

      var isValid = service.Verify("0901234567", code);

      Assert.False(isValid);
    }

    [Fact]
    public void GenerateAndStore_CalledTwiceForSamePhone_ShouldInvalidatePreviousCode()
    {
      var service = CreateService();
      var firstCode = service.GenerateAndStore("0901234567");
      service.GenerateAndStore("0901234567");

      var isValid = service.Verify("0901234567", firstCode);

      Assert.False(isValid);
    }
  }
}
