using AutoWashPro.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class SignalRBookingHubNotifierTests
  {
    [Fact]
    public async Task NotifySlotOccupancyChangedAsync_ShouldSendSlotOccupancyUpdatedToAllClients()
    {
      var clientsMock = new Mock<IHubClients>();
      var clientProxyMock = new Mock<IClientProxy>();
      clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

      var hubContextMock = new Mock<IHubContext<BookingHub>>();
      hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

      var notifier = new SignalRBookingHubNotifier(hubContextMock.Object);

      await notifier.NotifySlotOccupancyChangedAsync("2026-07-22", "09:00", 2, "Available");

      clientProxyMock.Verify(p => p.SendCoreAsync(
          "SlotOccupancyUpdated",
          It.IsAny<object[]>(),
          default), Times.Once);
    }
  }
}
