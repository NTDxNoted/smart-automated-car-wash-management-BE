using System.Threading.Tasks;
using AutoWash.Application.DTOs;
using AutoWash.Domain.Entities;

namespace AutoWash.Application.Interfaces
{
  public interface IBookingValidationService
  {
    Task ValidateCreateBookingAsync(CreateBookingRequest request, Customer? customer, Service service, string licensePlate);
  }
}
