using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.API.Data
{
    // Kế thừa DbContext của EF Core
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Sau này bạn sẽ khai báo các bảng (DbSet) ở đây
    }
}