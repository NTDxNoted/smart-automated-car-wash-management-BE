using System;
using System.Threading.Tasks;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/bookings")]
    [Authorize(Roles = "ADMIN")]
    public class AdminPaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public AdminPaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("{id}/payment")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentRequest request)
        {
            try
            {
                var result = await _paymentService.RecordPaymentAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message == "BOOKING_NOT_FOUND")
                {
                    return NotFound(new { error = "BOOKING_NOT_FOUND", message = "Không tìm thấy đơn đặt lịch" });
                }
                if (ex.Message == "ALREADY_PAID")
                {
                    return Conflict(new { error = "ALREADY_PAID", message = "Đơn hàng này đã được thanh toán" });
                }
                if (ex.Message == "CASH_NOT_CONFIRMED")
                {
                    return BadRequest(new { error = "CASH_NOT_CONFIRMED", message = "Nhân viên chưa xác nhận đã thu đủ tiền mặt" });
                }
                if (ex.Message == "INVALID_STATUS")
                {
                    return BadRequest(new { error = "INVALID_STATUS", message = "Chỉ có đơn đặt lịch ở trạng thái Pending mới được phép thanh toán" });
                }
                if (ex.Message == "INVALID_PAYMENT_METHOD")
                {
                    return BadRequest(new { error = "INVALID_PAYMENT_METHOD", message = "Phương thức thanh toán không hợp lệ" });
                }

                return BadRequest(new { error = "PAYMENT_FAILED", message = ex.Message });
            }
        }

        [HttpGet("{id}/transaction")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            try
            {
                var txn = await _paymentService.GetTransactionByBookingIdAsync(id);
                return Ok(new
                {
                    transactionId = txn.TransactionID,
                    bookingId = txn.BookingID,
                    amount = txn.Amount,
                    paymentMethod = txn.PaymentMethod.ToString(),
                    paidAt = txn.PaidAt,
                    status = txn.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                if (ex.Message == "TRANSACTION_NOT_FOUND")
                {
                    return NotFound(new { error = "TRANSACTION_NOT_FOUND", message = "Không tìm thấy thông tin giao dịch cho đơn hàng này" });
                }
                return BadRequest(new { error = "GET_TRANSACTION_FAILED", message = ex.Message });
            }
        }
    }
}
