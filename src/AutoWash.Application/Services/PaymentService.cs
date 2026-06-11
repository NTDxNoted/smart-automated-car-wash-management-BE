using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPointService _pointService;
        private readonly ITierService _tierService;

        public PaymentService(
            IApplicationDbContext context,
            IPointService pointService,
            ITierService tierService)
        {
            _context = context;
            _pointService = pointService;
            _tierService = tierService;
        }

        public async Task<PaymentResponse> RecordPaymentAsync(int bookingId, RecordPaymentRequest request)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            if (booking.Status != BookingStatus.Pending)
            {
                throw new Exception("INVALID_STATUS");
            }

            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethodEnum))
            {
                throw new Exception("INVALID_PAYMENT_METHOD");
            }

            if (request.Confirmed != true)
            {
                if (paymentMethodEnum == PaymentMethod.Cash)
                    throw new Exception("CASH_NOT_CONFIRMED");
                else
                    throw new Exception("TRANSFER_NOT_CONFIRMED");
            }

            if (_context is DbContext dbContext)
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // PaymentMethodEnum is determined from the request parsing above

                    var dbTransaction = new Transaction
                    {
                        BookingID = booking.BookingID,
                        Amount = booking.FinalAmount,
                        PaymentMethod = paymentMethodEnum,
                        PaidAt = DateTime.UtcNow, // BR-46: Server timestamp
                        Status = TransactionStatus.Paid
                    };

                    await _context.Transactions.AddAsync(dbTransaction);
                    
                    // SaveChangesAsync here to trigger the Unique index violation early if double paid
                    await _context.SaveChangesAsync();

                    booking.Status = BookingStatus.Completed;
                    booking.CompletedAt = DateTime.UtcNow;

                    var customer = await _context.Customers.FindAsync(booking.CustomerID);
                    if (customer != null)
                    {
                        customer.TotalSpending += booking.FinalAmount;
                        customer.LastVisit = DateTime.UtcNow;
                    }

                    int pointsEarned = await _pointService.EarnPointsAsync(booking.BookingID);

                    // Save all modifications to Loyalty and Customer before evaluating Tier
                    await _context.SaveChangesAsync();

                    if (customer != null)
                    {
                        await _tierService.EvaluateUpgradeAsync(customer.CustomerID);
                    }

                    await transaction.CommitAsync();

                    int totalPointsAfter = 0;
                    string tierAfter = "Member";
                    if (customer != null)
                    {
                        var loyalty = await _context.LoyaltyAccounts
                            .FirstOrDefaultAsync(l => l.CustomerID == customer.CustomerID);
                        totalPointsAfter = loyalty?.TotalPoints ?? 0;

                        var tier = await _context.Tiers.FindAsync(customer.TierID);
                        tierAfter = tier?.TierName ?? "Member";
                    }

                    return new PaymentResponse
                    {
                        TransactionId = dbTransaction.TransactionID,
                        BookingId = booking.BookingID,
                        Amount = dbTransaction.Amount,
                        PaymentMethod = dbTransaction.PaymentMethod.ToString(),
                        PaidAt = dbTransaction.PaidAt,
                        Status = dbTransaction.Status.ToString(),
                        BookingStatus = booking.Status.ToString(),
                        Loyalty = new PaymentLoyaltyInfo
                        {
                            PointsEarned = pointsEarned,
                            TotalPointsAfter = totalPointsAfter,
                            TierAfter = tierAfter
                        }
                    };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();

                    var inner = ex.InnerException;
                    if (inner != null && inner.GetType().Name == "PostgresException")
                    {
                        var sqlStateProp = inner.GetType().GetProperty("SqlState");
                        var sqlState = sqlStateProp?.GetValue(inner) as string;
                        if (sqlState == "23505")
                        {
                            throw new Exception("ALREADY_PAID");
                        }
                    }
                    throw;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                throw new Exception("DATABASE_TRANSACTION_ERROR");
            }
        }

        public async Task<Transaction> GetTransactionByBookingIdAsync(int bookingId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.BookingID == bookingId);

            if (transaction == null) throw new Exception("TRANSACTION_NOT_FOUND");
            return transaction;
        }
    }
}
