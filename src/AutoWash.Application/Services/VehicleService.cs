using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Application.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IApplicationDbContext _context;
        private readonly IOtpService _otpService;

        public VehicleService(IApplicationDbContext context, IOtpService otpService)
        {
            _context = context;
            _otpService = otpService;
        }

        public async Task<IEnumerable<VehicleResponse>> GetVehiclesAsync(int customerId)
        {
            return await _context.Vehicles
                .Where(v => v.CustomerID == customerId && v.IsActive)
                .Select(v => new VehicleResponse
                {
                    VehicleId = v.VehicleID,
                    CustomerId = v.CustomerID,
                    LicensePlate = v.LicensePlate,
                    IsActive = v.IsActive,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<VehicleResponse> AddVehicleAsync(int customerId, AddVehicleRequest request)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy tài khoản.");

            // BR-10: verify OTP
            if (!_otpService.Verify(customer.Phone, request.OtpCode))
                throw new Exception("INVALID_OTP: Mã OTP không hợp lệ hoặc đã hết hạn.");

            // BR-09: UNIQUE (CustomerID, LicensePlate)
            var exists = await _context.Vehicles.AnyAsync(v =>
                v.CustomerID == customerId && v.LicensePlate == request.LicensePlate && v.IsActive);
            if (exists) throw new Exception("PLATE_ALREADY_SAVED");

            var vehicle = new Vehicle
            {
                CustomerID = customerId,
                LicensePlate = request.LicensePlate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return MapToResponse(vehicle);
        }

        public async Task<VehicleResponse> UpdateVehicleAsync(int customerId, int vehicleId, UpdateVehicleRequest request)
        {
            // BR-11: chỉ xe của chính customer
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v =>
                v.VehicleID == vehicleId && v.CustomerID == customerId && v.IsActive);
            if (vehicle == null) throw new Exception("NOT_FOUND: Không tìm thấy xe.");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy tài khoản.");

            // BR-10: verify OTP
            if (!_otpService.Verify(customer.Phone, request.OtpCode))
                throw new Exception("INVALID_OTP: Mã OTP không hợp lệ hoặc đã hết hạn.");

            // BR-09: check duplicate trong cùng tài khoản
            var duplicate = await _context.Vehicles.AnyAsync(v =>
                v.CustomerID == customerId && v.LicensePlate == request.LicensePlate
                && v.VehicleID != vehicleId && v.IsActive);
            if (duplicate) throw new Exception("PLATE_ALREADY_SAVED");

            vehicle.LicensePlate = request.LicensePlate;
            await _context.SaveChangesAsync();
            return MapToResponse(vehicle);
        }

        public async Task DeleteVehicleAsync(int customerId, int vehicleId)
        {
            // BR-11: chỉ xe của chính customer
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v =>
                v.VehicleID == vehicleId && v.CustomerID == customerId && v.IsActive);
            if (vehicle == null) throw new Exception("NOT_FOUND: Không tìm thấy xe.");

            vehicle.IsActive = false;
            await _context.SaveChangesAsync();
        }

        private static VehicleResponse MapToResponse(Vehicle v) => new VehicleResponse
        {
            VehicleId = v.VehicleID,
            CustomerId = v.CustomerID,
            LicensePlate = v.LicensePlate,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        };
    }
}
