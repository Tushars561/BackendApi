using Microsoft.EntityFrameworkCore;
using WebApplication1.Entity;

namespace WebApplication1
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;

       
    }
}
