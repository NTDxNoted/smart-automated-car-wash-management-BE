using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
  public interface IInvoiceService
  {
    BookingInvoiceDto CalculateInvoice(decimal baseAmount, int tierId, int? rewardId, decimal? rewardDiscountAmount, string? promoCode, decimal? promotionDiscountAmount);
  }
}
