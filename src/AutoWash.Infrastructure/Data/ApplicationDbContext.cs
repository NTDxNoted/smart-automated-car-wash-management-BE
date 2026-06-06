using Microsoft.EntityFrameworkCore;

namespace AutoWash.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Sau này khai báo các DbSet (bảng) ở đây
    }
}