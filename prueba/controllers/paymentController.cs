using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaypalApi.Models;
using PaypalApi.Context;

namespace PaypalApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly TransactionLogger _logger;

        public PaymentsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _logger = new TransactionLogger();
        }

        // Clase para los parámetros de consulta
        public class PaymentQueryParameters
        {
            private const int MaxPageSize = 50;
            private int _pageSize = 10;

            public int PageNumber { get; set; } = 1;
            public int PageSize
            {
                get => _pageSize;
                set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
            }

            private string? _status;
            public string? Status
            {
                get => _status;
                set => _status = NormalizeStatus(value);
            }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            private string? NormalizeStatus(string? status)
            {
                if (string.IsNullOrWhiteSpace(status)) return null;

                return status.ToUpper() switch
                {
                    "S" or "SUCCESS" or "SUCCESSFUL" => "S",
                    "P" or "PENDING" => "P",
                    "V" or "REVERSED" => "V",
                    "F" or "FAILED" => "F",
                    _ => null
                };
            }
        }

        // Clase para la respuesta paginada
        public class PagedResponse<T>
        {
            public IEnumerable<T> Data { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public int TotalRecords { get; set; }

            public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalRecords)
            {
                Data = data;
                PageNumber = pageNumber;
                PageSize = pageSize;
                TotalRecords = totalRecords;
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            }
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<PaymentNotification>>> GetAllPayments([FromQuery] PaymentQueryParameters queryParams)
        {
            try
            {
                IQueryable<PaymentNotification> query = _dbContext.PaymentsNotifications;

                // Aplicar filtros
                if (!string.IsNullOrWhiteSpace(queryParams.Status))
                {
                    query = query.Where(p => p.Status == queryParams.Status);
                }

                if (queryParams.StartDate.HasValue)
                {
                    query = query.Where(p => p.Date >= queryParams.StartDate.Value.Date);
                }

                if (queryParams.EndDate.HasValue)
                {
                    query = query.Where(p => p.Date <= queryParams.EndDate.Value.Date);
                }

                // Obtener total antes de la paginación
                var totalRecords = await query.CountAsync();

                if (totalRecords == 0)
                {
                    var errorMessage = BuildErrorMessage(queryParams);
                    await _logger.LogTransaction(errorMessage, false);
                    return NotFound(new
                    {
                        message = errorMessage,
                        validStatuses = new Dictionary<string, string>
                        {
                            { "S", "Success" },
                            { "P", "Pending" },
                            { "V", "Reversed" },
                            { "F", "Failed" }
                        },
                        appliedFilters = new
                        {
                            status = queryParams.Status,
                            startDate = queryParams.StartDate?.ToString("yyyy-MM-dd"),
                            endDate = queryParams.EndDate?.ToString("yyyy-MM-dd"),
                            pageNumber = queryParams.PageNumber,
                            pageSize = queryParams.PageSize
                        }
                    });
                }

                // Aplicar ordenamiento y paginación
                query = query.OrderByDescending(p => p.Date)
                            .ThenByDescending(p => p.Time);

                var payments = await query
                    .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToListAsync();

                var pagedResponse = new PagedResponse<PaymentNotification>(
                    payments,
                    queryParams.PageNumber,
                    queryParams.PageSize,
                    totalRecords
                );

                await _logger.LogTransaction($"Se recuperaron {payments.Count} notificaciones de pago (Página {queryParams.PageNumber})", true);
                return Ok(pagedResponse);
            }
            catch (Exception ex)
            {
                await _logger.LogTransaction($"Error al obtener las notificaciones de pago: {ex.Message}", false);
                return StatusCode(500, new
                {
                    message = "Error interno al obtener las notificaciones de pago.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentNotification>> GetPaymentById(int id)
        {
            try
            {
                var payment = await _dbContext.PaymentsNotifications
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (payment == null)
                {
                    await _logger.LogTransaction($"Notificación de pago con ID {id} no encontrada", false);
                    return NotFound(new
                    {
                        message = $"No se encontró la notificación de pago con ID {id}.",
                        id = id
                    });
                }

                await _logger.LogTransaction($"Notificación de pago con ID {id} recuperada exitosamente", true);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                await _logger.LogTransaction($"Error al obtener la notificación de pago {id}: {ex.Message}", false);
                return StatusCode(500, new
                {
                    message = $"Error interno al obtener la notificación de pago con ID {id}.",
                    error = ex.Message
                });
            }
        }

        private string BuildErrorMessage(PaymentQueryParameters queryParams)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(queryParams.Status))
            {
                filters.Add($"estado '{queryParams.Status}'");
            }
            if (queryParams.StartDate.HasValue)
            {
                filters.Add($"fecha inicial '{queryParams.StartDate.Value:yyyy-MM-dd}'");
            }
            if (queryParams.EndDate.HasValue)
            {
                filters.Add($"fecha final '{queryParams.EndDate.Value:yyyy-MM-dd}'");
            }

            if (filters.Count == 0)
            {
                return "No se encontraron notificaciones de pago en la base de datos.";
            }

            return $"No se encontraron notificaciones de pago con los siguientes filtros: {string.Join(", ", filters)}.";
        }
    }
}