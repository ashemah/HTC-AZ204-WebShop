using Microsoft.EntityFrameworkCore;

namespace Contoso.Api.Models
{
    public class ContosoDbContext : DbContext
    {
        public ContosoDbContext(DbContextOptions<ContosoDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasNoDiscriminator()
                .ToContainer("Orders")
                .HasPartitionKey(da => da.Id)
                .HasKey(da => da.Id );

            modelBuilder.Entity<User>()
                .HasNoDiscriminator()
                .ToContainer("Users")
                .HasPartitionKey(da => da.Email)
                .HasKey(da => da.Id );

            modelBuilder.Entity<Product>()
                .HasNoDiscriminator()
                .ToContainer("Products")
                .HasPartitionKey(da => da.Category)
                .HasKey(da => da.Id );
        }
    }
}