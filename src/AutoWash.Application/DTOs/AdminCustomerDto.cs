using System;
using System.Collections.Generic;

namespace AutoWash.Application.DTOs
{
    // DTO cho danh sách GET /api/admin/customers
    public class CustomerAdminResponseDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public int Points { get; set; }
        public decimal TotalSpending { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? SuspendedUntil { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsWalkIn { get; set; }
        public int BookingCount { get; set; }
        public DateTime? LastVisit { get; set; }
        public string? AdminNotes { get; set; }
    }

    // DTO cho PATCH /api/admin/customers/{id}/lock
    public class LockCustomerResponseDto
    {
        public int CustomerId { get; set; }
        public bool IsLocked { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // DTO cho PATCH /api/admin/customers/{id}/notes
    public class UpdateCustomerNotesRequest
    {
        public string? Notes { get; set; }
    }

    // Item lịch sử booking hiển thị trong trang chi tiết khách hàng — DTO riêng (không dùng chung
    // BookingResponseDto vì DTO đó còn phục vụ endpoint booking phía member).
    public class CustomerBookingHistoryItemDto
    {
        public int BookingId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public ServiceResponse Service { get; set; } = new();
        public string? PaymentMethod { get; set; }
        public string? PromotionApplied { get; set; }
        public string? InvoiceCode { get; set; }
    }

    // DTO cho chi tiết khách hàng GET /api/admin/customers/{id}
    public class CustomerDetailAdminResponseDto : CustomerAdminResponseDto
    {
        // Gắn thêm lịch sử booking
        public List<CustomerBookingHistoryItemDto> BookingHistory { get; set; } = new List<CustomerBookingHistoryItemDto>();
        // Gắn thêm lịch sử điểm (nếu cần)
        // public List<PointTransactionDto> PointHistory { get; set; }
    }
}