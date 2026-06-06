using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWash.Application.Services
{
  public class InvoiceService : IInvoiceService
  {
    private const decimal DefaultRewardDiscountRate = 0.5m;

    public BookingInvoiceDto CalculateInvoice(decimal baseAmount, int tierId, int? rewardId, decimal? rewardDiscountAmount, string? promoCode, decimal? promotionDiscountAmount)
    {
      var tierDiscountRate = GetTierDiscountRate(tierId);
      var tierDiscount = Math.Round(baseAmount * tierDiscountRate, 0);
      var rewardDiscount = rewardDiscountAmount ?? 0;
      var promotionDiscount = promotionDiscountAmount ?? 0;
      var discountApplied = tierDiscount + rewardDiscount + promotionDiscount;
      var finalAmount = Math.Max(baseAmount - discountApplied, 0);

      return new BookingInvoiceDto
      {
        BaseAmount = baseAmount,
        TierDiscount = tierDiscount,
        RewardDiscount = rewardDiscount,
        PromotionDiscount = promotionDiscount,
        DiscountApplied = discountApplied,
        FinalAmount = finalAmount
      };
    }

    private static decimal GetTierDiscountRate(int tierId)
    {
      return tierId switch
      {
        1 => 0.05m,
        2 => 0.10m,
        _ => 0.03m
      };
    }
  }
}
