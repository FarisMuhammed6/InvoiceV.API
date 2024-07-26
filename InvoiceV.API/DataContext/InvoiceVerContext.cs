using InvoiceV.API.Entites;
using Microsoft.EntityFrameworkCore;

namespace InvoiceV.API.DataContext
{
    public class InvoiceVerContext : DbContext
    {
        public InvoiceVerContext(DbContextOptions<InvoiceVerContext> options) : base(options)
        {

        }
        public DbSet<Contract> contracts { get; set; }
        public DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure precision and scale for decimal properties in Contract entity
            modelBuilder.Entity<Contract>()
                .Property(c => c.Amount)
                .HasColumnType("decimal(18,2)");

            // Configure precision and scale for decimal properties in Invoice entity
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasColumnType("decimal(18,2)");
        }

    }
}
