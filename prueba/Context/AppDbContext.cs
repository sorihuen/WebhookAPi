using Microsoft.EntityFrameworkCore;
using PaypalApi.Models;

namespace PaypalApi.Context

{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<PaymentNotification> PaymentsNotifications { get; set; }


        
    }
}
