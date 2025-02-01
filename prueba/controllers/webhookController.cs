using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaypalApi.Models;

namespace PaypalApi.Controllers
{
    public class WebHookController : Controller
    {
        [HttpPost("api/checkout/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Post([FromQuery] string? start_date = null, [FromQuery] string? end_date = null)
        {
            const SecurityProtocolType tls13 = (SecurityProtocolType)12288;
            ServicePointManager.SecurityProtocol = tls13 | SecurityProtocolType.Tls12;

            try
            {
                // Get PayPal access token
                var token = await GetPayPalAccessToken();
                if (string.IsNullOrEmpty(token?.access_token))
                {
                    return BadRequest("Error: No se pudo obtener el token de acceso.");
                }

                // Get transaction history with proper date range
                var startDateTime = start_date != null 
                    ? DateTime.Parse(start_date)
                    : DateTime.UtcNow.AddHours(-7); // Ajustamos para cubrir más horas del día

                var endDateTime = end_date != null
                    ? DateTime.Parse(end_date).AddDays(1).AddSeconds(-1)
                    : DateTime.UtcNow;

                var transactionData = await GetTransactionHistory(token.access_token, startDateTime, endDateTime);
                
                
                // Return the transaction data
                return Content(transactionData, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private async Task<TokenJson> GetPayPalAccessToken()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en_US"));

                var clientId = "AcO_aCfae6xsgJfokhz1BwqKiueq8K0WNdj7KNL2BmL79lRyS79WvuEjKlFRoxRDEyOTXFfYTNjVMgDo";
                var clientSecret = "EC280bix79Gd4QV_Cnb7cMsllVKQIjDBldt2aK7RCIRJLw7YRf2OGO6ZqR110Q0cMSCi_5WLnMQinvq7";
                var bytes = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));

                var keyValues = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                };

                var responseMessage = await client.PostAsync("https://api-m.sandbox.paypal.com/v1/oauth2/token", new FormUrlEncodedContent(keyValues));
                var response = await responseMessage.Content.ReadAsStringAsync();
                //Console.WriteLine("Token Response: " + response);

                return JsonConvert.DeserializeObject<TokenJson>(response) ?? new TokenJson();
            }
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