using System.Text.Json;
using PaypalApi.Models;
using Microsoft.Extensions.Logging;

namespace PaypalApi.Services
{
    // Servicio encargado de procesar los pagos de PayPal
    public class PaymentProcessorService : IPaymentProcessorService
    {
        private readonly ILogger<PaymentProcessorService> _logger;

        // Constructor para inyectar el logger
        public PaymentProcessorService(ILogger<PaymentProcessorService> logger)
        {
            _logger = logger;
        }

        // Método privado que procesa el monto de una transacción a partir de un string
        private decimal ProcessAmount(string amountString)
        {
            try
            {
                // Limpiar el string de entrada eliminando símbolos no numéricos
                amountString = amountString.Trim()
                    .Replace("$", "")
                    .Replace(" ", "");

                // Registro de la operación de procesamiento del monto
                _logger.LogInformation("Procesando monto: {Amount}", amountString);

                // Manejo del formato de la cantidad en caso de que contenga comas
                if (amountString.Contains(","))
                {
                    // Si contiene punto y coma, asumimos que la coma es separador de miles
                    if (amountString.Contains("."))
                    {
                        amountString = amountString.Replace(",", "");
                    }
                    // Si solo tiene coma, asumimos que es separador decimal
                    else
                    {
                        amountString = amountString.Replace(",", ".");
                    }
                }

                // Intentar parsear el monto a un decimal
                if (decimal.TryParse(amountString,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal parsedAmount))
                {
                    // Redondear el monto a dos decimales
                    var finalAmount = Math.Round(parsedAmount, 2);
                    _logger.LogInformation("Monto procesado exitosamente: {OriginalAmount} -> {ProcessedAmount}",
                        amountString, finalAmount);
                    return finalAmount;
                }

                _logger.LogWarning("No se pudo procesar el monto: {Amount}", amountString);
                return 0m; // Retornar 0 si no se puede procesar el monto
            }
            catch (Exception ex)
            {
                // Registro del error si algo falla en el procesamiento
                _logger.LogError(ex, "Error procesando monto: {Amount}", amountString);
                return 0m;
            }
        }

        // Método público que procesa las transacciones de PayPal a partir de una respuesta en formato JSON
        public List<PaymentNotification> ProcessPayPalTransactions(string jsonResponse)
        {
            var paymentNotifications = new List<PaymentNotification>();

            // Parsear la respuesta JSON
            using (JsonDocument document = JsonDocument.Parse(jsonResponse))
            {
                var root = document.RootElement;
                var transactions = root.GetProperty("transaction_details").EnumerateArray();

                // Iterar sobre todas las transacciones
                foreach (var transaction in transactions)
                {
                    var transactionInfo = transaction.GetProperty("transaction_info");
                    var eventCode = transactionInfo.GetProperty("transaction_event_code").GetString() ?? "Unknown";

                    if (eventCode != "Unknown")
                    {
                        // Extraer información de la transacción
                        var transactionAmount = transactionInfo.GetProperty("transaction_amount")
                            .GetProperty("value").GetString();
                        var transactionStatus = transactionInfo.GetProperty("transaction_status").GetString();
                        var transactionId = transactionInfo.GetProperty("transaction_id").GetString();

                        // Obtener descripción de la transacción
                        var description = GetTransactionDescription(transaction);

                        // Loguear los detalles de la transacción
                        _logger.LogInformation(
                            "Transaction Code detected - Code: {EventCode}, TransactionId: {TransactionId}, Amount: {Amount}, Status: {Status}, Description: {Description}",
                            eventCode, transactionId, transactionAmount, transactionStatus, description);
                    }

                    // Convertir la fecha de la transacción
                    var dateTimeStr = transactionInfo.GetProperty("transaction_initiation_date").GetString();
                    var dateTime = DateTime.Parse(dateTimeStr!);

                    // Procesar el monto
                    var amount = transactionInfo.GetProperty("transaction_amount");
                    var amountString = amount.GetProperty("value").GetString() ?? "0";
                    var processedAmount = ProcessAmount(amountString);

                    // Determinar el método de pago y el banco involucrado
                    var (paymentMethod, bank) = DeterminePaymentMethod(transaction, eventCode);

                    // Crear un objeto de notificación de pago con la información procesada
                    var notification = new PaymentNotification
                    {
                        Date = dateTime.Date,
                        Time = dateTime.TimeOfDay,
                        TransactionId = transactionInfo.GetProperty("transaction_id").GetString()!,
                        Status = MapPayPalStatus(transactionInfo.GetProperty("transaction_status").GetString()!),
                        Amount = processedAmount,
                        PaymentMethod = paymentMethod,
                        Bank = bank
                    };

                    // Agregar la notificación a la lista
                    paymentNotifications.Add(notification);
                }
            }

            // Retornar la lista de notificaciones de pago
            return paymentNotifications;
        }

        // Método privado que obtiene la descripción de la transacción
        private string GetTransactionDescription(JsonElement transaction)
        {
            var description = new List<string>();

            // Verificar si existen propiedades de descripción dentro de la transacción
            if (transaction.TryGetProperty("transaction_info", out var transactionInfo))
            {
                if (transactionInfo.TryGetProperty("transaction_subject", out var subject))
                {
                    description.Add($"Subject: {subject.GetString()}");
                }
                if (transactionInfo.TryGetProperty("transaction_note", out var note))
                {
                    description.Add($"Note: {note.GetString()}");
                }
            }

            if (transaction.TryGetProperty("payer_info", out var payerInfo))
            {
                if (payerInfo.TryGetProperty("email_address", out var email))
                {
                    description.Add($"Payer: {email.GetString()}");
                }
            }

            // Unir todos los elementos de la descripción en un solo string
            return string.Join(" | ", description);
        }

        // Método privado que determina el método de pago y el banco de la transacción
        private static (string PaymentMethod, string Bank) DeterminePaymentMethod(JsonElement transaction, string? eventCode)
        {
            // Manejar el caso específico para el código de evento T1900 (Balance Inicial)
            if (!string.IsNullOrEmpty(eventCode) && eventCode == "T1900")
            {
                return ("Initial Balance", "PayPal");
            }

            // Para otros códigos de evento, usar la lógica existente
            if (transaction.TryGetProperty("cart_info", out JsonElement cartInfo) &&
                cartInfo.ValueKind != JsonValueKind.Null &&
                cartInfo.TryGetProperty("item_details", out var itemDetails) &&
                itemDetails.ValueKind == JsonValueKind.Array &&
                itemDetails.GetArrayLength() > 0)
            {
                return ("Credit Card", "Card Payment");
            }

            if (transaction.GetProperty("payer_info").TryGetProperty("email_address", out JsonElement emailElement) &&
                emailElement.ValueKind != JsonValueKind.Null)
            {
                return ("PayPal Balance", "PayPal");
            }

            // Si no se puede determinar, retornar valores desconocidos
            return ("Unknown", "Unknown");
        }

        // Método privado que mapea el estado de PayPal a un estado comprensible
        private static string MapPayPalStatus(string paypalStatus)
        {
            return paypalStatus switch
            {
                "S" => "Success",
                "P" => "Pending",
                "V" => "Reversed",
                "F" => "Failed",
                _ => "Unknown"
            };
        }
    }
}
