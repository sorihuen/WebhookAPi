using Microsoft.EntityFrameworkCore;
using PaypalApi.Models;
using Auth.Models;

namespace PaypalApi.Context

{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<PaymentNotification> PaymentsNotifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración para la propiedad Amount
            modelBuilder.Entity<PaymentNotification>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        }



    }
}
