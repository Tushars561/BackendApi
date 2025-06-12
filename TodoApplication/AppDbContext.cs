using Microsoft.EntityFrameworkCore;
using WebApplication1.Entity;

namespace WebApplication1
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;

        internal User SingleOrDefault(Func<object, bool> value)
        {
            throw new NotImplementedException();
        }

        public DbSet<TodoItem> TodoItems { get; set; } = null!;
        public DbSet<Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.User)
                .WithMany(u => u.TodoItems)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Optional: deletes todos if user is deleted
        }

    }
}
