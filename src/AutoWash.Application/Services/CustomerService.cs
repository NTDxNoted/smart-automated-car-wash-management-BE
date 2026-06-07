using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWash.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IApplicationDbContext _context;

        public CustomerService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProfileResponse> GetProfileAsync(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy tài khoản.");

            var loyalty = await _context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerID == customerId);

            return new ProfileResponse
            {
                CustomerId = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Tier = customer.Tier,
                TotalSpending = customer.TotalSpending,
                LoyaltyPoints = loyalty?.TotalPoints ?? 0,
                LastVisit = customer.LastVisit,
                SuspendedUntil = customer.SuspendedUntil
            };
        }

        public async Task<ProfileResponse> UpdateProfileAsync(int customerId, UpdateProfileRequest request)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy tài khoản.");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                customer.FullName = request.FullName;

            await _context.SaveChangesAsync();
            return await GetProfileAsync(customerId);
        }
    }
}
