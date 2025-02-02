using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PaypalApi.Models;
using PaypalApi.Services;
using PaypalApi.Context;


namespace PaypalApi.Controllers

{
    public class WebHookController : Controller
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly HttpClient _httpClient;

        private readonly IPaymentProcessorService _paymentProcessor;
        private readonly AppDbContext _dbContext;
        private readonly TransactionLogger _logger;

        public WebHookController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IPaymentProcessorService paymentProcessor, AppDbContext dbContext)
        {
            _logger = new TransactionLogger();
            _clientId = configuration["PayPal:clientId"] ?? throw new ArgumentNullException(nameof(_clientId));
            _clientSecret = configuration["PayPal:clientSecret"] ?? throw new ArgumentNullException(nameof(_clientSecret));
            _httpClient = httpClientFactory.CreateClient();
            _paymentProcessor = paymentProcessor;
            _dbContext = dbContext;
        }

        [HttpPost("api/checkout/webhook")]
        [Authorize]
        public async Task<IActionResult> Post([FromQuery] string? start_date = null, [FromQuery] string? end_date = null)
        {
            const SecurityProtocolType tls13 = (SecurityProtocolType)12288;
            ServicePointManager.SecurityProtocol = tls13 | SecurityProtocolType.Tls12;

            try
            {
                await _logger.LogTransaction("Iniciando proceso de webhook PayPal", true);
                // Get PayPal access token
                var token = await GetPayPalAccessToken();
                if (string.IsNullOrEmpty(token?.access_token))
                {
                    await _logger.LogTransaction("Error al obtener token de acceso de PayPal", false);
                    return BadRequest("Error: No se pudo obtener el token de acceso.");
                }
                await _logger.LogTransaction("Token de acceso obtenido exitosamente", true);

                var startDateTime = start_date != null
                    ? DateTime.Parse(start_date)
                    : DateTime.UtcNow.AddHours(-7);

                var endDateTime = end_date != null
                    ? DateTime.Parse(end_date).AddDays(1).AddSeconds(-1)
                    : DateTime.UtcNow;

                var transactionData = await GetTransactionHistory(token.access_token, startDateTime, endDateTime);
                await _logger.LogTransaction($"Transacciones obtenidas para el período {startDateTime} - {endDateTime}", true);

                // Procesar las transacciones usando el servicio
                var payments = _paymentProcessor.ProcessPayPalTransactions(transactionData);
                await _logger.LogTransaction($"Procesadas {payments.Count()} transacciones", true);

                // Obtener las transacciones existentes en la base de datos
                var existingTransactionIds = await _dbContext.PaymentsNotifications
                    .Where(p => payments.Select(payment => payment.TransactionId).Contains(p.TransactionId))
                    .Select(p => p.TransactionId)
                    .ToListAsync();

                // Filtrar las transacciones nuevas
                var newPayments = payments.Where(payment => !existingTransactionIds.Contains(payment.TransactionId)).ToList();

                // Insertar las nuevas transacciones
                if (newPayments.Any())
                {
                    await _dbContext.PaymentsNotifications.AddRangeAsync(newPayments);
                    await _dbContext.SaveChangesAsync();
                    await _logger.LogTransaction($"Guardadas {newPayments.Count} nuevas transacciones en la base de datos", true);
                }
                else
                {
                    await _logger.LogTransaction("No se encontraron nuevas transacciones para guardar", true);
                }

                // Obtener todos los pagos actualizados de la base de datos
                var updatedPayments = await _dbContext.PaymentsNotifications
                    .Where(p => payments.Select(x => x.TransactionId).Contains(p.TransactionId))
                    .OrderByDescending(p => p.Date)
                    .ThenByDescending(p => p.Time)
                    .ToListAsync();
                await _logger.LogTransaction("Proceso completado exitosamente", true);
                return Ok(updatedPayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
        private async Task<TokenJson> GetPayPalAccessToken()
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en_US"));

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var keyValues = new List<KeyValuePair<string, string>>()
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials")
    };

            var responseMessage = await _httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/oauth2/token", new FormUrlEncodedContent(keyValues));
            var response = await responseMessage.Content.ReadAsStringAsync();

            //Console.WriteLine("Token Response: " + response);

            return JsonConvert.DeserializeObject<TokenJson>(response) ?? new TokenJson();
        }


        private async Task<string> GetTransactionHistory(string accessToken, DateTime startDate, DateTime endDate)
        {
            // Format dates in PayPal's expected format
            var start = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var end = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var transactionHistoryUrl = $"https://api-m.sandbox.paypal.com/v1/reporting/transactions?start_date={start}&end_date={end}&fields=all&page_size=100";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en_US"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var responseMessage = await client.GetAsync(transactionHistoryUrl);
                var response = await responseMessage.Content.ReadAsStringAsync();
                //Console.WriteLine("Transaction API Response: " + response);

                return response;
            }
        }
    }
}