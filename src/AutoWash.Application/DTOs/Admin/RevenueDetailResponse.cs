namespace AutoWash.Application.DTOs.Admin
{
  public class RevenueDetailResponse
  {
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string PaymentMethodFilter { get; set; } = "All";
    public decimal GrossRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal CashRevenue { get; set; }
    public decimal TransferRevenue { get; set; }
    public List<RevenueTransactionItemDto> Transactions { get; set; } = new();
  }

  public class RevenueTransactionItemDto
  {
    public string InvoiceCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PromotionApplied { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
  }
}
