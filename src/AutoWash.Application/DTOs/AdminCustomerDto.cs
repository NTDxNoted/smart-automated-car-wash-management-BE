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
    }

    // DTO cho PATCH /api/admin/customers/{id}/lock
    public class LockCustomerResponseDto
    {
        public int CustomerId { get; set; }
        public bool IsLocked { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // DTO cho chi tiết khách hàng GET /api/admin/customers/{id}
    public class CustomerDetailAdminResponseDto : CustomerAdminResponseDto
    {
        // Gắn thêm lịch sử booking
        public List<BookingResponseDto> BookingHistory { get; set; } = new List<BookingResponseDto>();
        // Gắn thêm lịch sử điểm (nếu cần)
        // public List<PointTransactionDto> PointHistory { get; set; }
    }
}