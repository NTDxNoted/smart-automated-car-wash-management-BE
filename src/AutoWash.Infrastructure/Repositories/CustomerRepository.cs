using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Infrastructure.Repositories
{
  public class CustomerRepository
  {
    private readonly IApplicationDbContext _context;

    public CustomerRepository(IApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
      return await _context.Customers.SingleOrDefaultAsync(c => c.Phone == phone);
    }

    public async Task<bool> PhoneExistsAsync(string phone)
    {
      return await _context.Customers.AnyAsync(c => c.Phone == phone);
    }

    public async Task<Customer> AddAsync(Customer customer)
    {
      _context.Customers.Add(customer);
      await _context.SaveChangesAsync();
      return customer;
    }
  }
}
