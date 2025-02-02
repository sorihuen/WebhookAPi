using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaypalApi.Context;


namespace PaypalApi.Controllers
{
    [Authorize]
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats()
        {
            try
            {
                // Obtener total de transacciones exitosas
                var successfulTransactions = await _context.PaymentsNotifications
                    .Where(p => p.Status.ToLower() == "success")
                    .CountAsync();

                // Calcular el monto total de todas las transacciones exitosas
                var totalAmount = await _context.PaymentsNotifications
                    .Where(p => p.Status.ToLower() == "success")
                    .SumAsync(p => p.Amount);

                // Obtener los métodos de pago más utilizados (top 5)
                var topPaymentMethods = await _context.PaymentsNotifications
                    .Where(p => p.Status.ToLower() == "success")
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new PaymentMethodStats
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                var dashboardStats = new DashboardStats
                {
                    SuccessfulTransactions = successfulTransactions,
                    TotalAmount = totalAmount,
                    TopPaymentMethods = topPaymentMethods
                };

                return Ok(dashboardStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }

    public class DashboardStats
    {
        public int SuccessfulTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public required List<PaymentMethodStats> TopPaymentMethods { get; set; }
    }

    public class PaymentMethodStats
    {
        public required string Method { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }

        // Constructor para inicializar las propiedades requeridas
        public PaymentMethodStats()
        {
            Method = string.Empty;
        }
    }
}