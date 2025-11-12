using BlazorDataGrid.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorDataGrid.Data
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions<MyContext> options)
            : base(options)
        {
        }

        public DbSet<Blog> Blogs => Set<Blog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔎 Filtro globale per Soft Delete
            modelBuilder.Entity<Blog>().HasQueryFilter(b => !b.IsDeleted);
        }
    }
}