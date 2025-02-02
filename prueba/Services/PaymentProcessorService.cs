using System.Text.Json;
using PaypalApi.Models;
using Microsoft.Extensions.Logging;

namespace PaypalApi.Services
{
    public class PaymentProcessorService : IPaymentProcessorService
    {
        private readonly ILogger<PaymentProcessorService> _logger;

        public PaymentProcessorService(ILogger<PaymentProcessorService> logger)
        {
            _logger = logger;
        }

        public List<PaymentNotification> ProcessPayPalTransactions(string jsonResponse)
        {
            var paymentNotifications = new List<PaymentNotification>();

            using (JsonDocument document = JsonDocument.Parse(jsonResponse))
            {
                var root = document.RootElement;
                var transactions = root.GetProperty("transaction_details").EnumerateArray();

                foreach (var transaction in transactions)
                {
                    var transactionInfo = transaction.GetProperty("transaction_info");
                    var eventCode = transactionInfo.GetProperty("transaction_event_code").GetString() ?? "Unknown";

                    if (eventCode != "Unknown")
                    {
                        var transactionAmount = transactionInfo.GetProperty("transaction_amount")
                            .GetProperty("value").GetString();
                        var transactionStatus = transactionInfo.GetProperty("transaction_status").GetString();
                        var transactionId = transactionInfo.GetProperty("transaction_id").GetString();

                        var description = GetTransactionDescription(transaction);

                        _logger.LogInformation(
                            "Transaction Code detected - Code: {EventCode}, TransactionId: {TransactionId}, Amount: {Amount}, Status: {Status}, Description: {Description}",
                            eventCode, transactionId, transactionAmount, transactionStatus, description);
                    }

                    var dateTimeStr = transactionInfo.GetProperty("transaction_initiation_date").GetString();
                    var dateTime = DateTime.Parse(dateTimeStr!);

                    // Manejo mejorado del monto
                    var amount = transactionInfo.GetProperty("transaction_amount");
                    var amountString = amount.GetProperty("value").GetString();
                    // Asegurarse de que el parsing use el formato invariante y mantenga los decimales
                    var amountValue = decimal.Parse(amountString!, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.InvariantCulture);

                    var (paymentMethod, bank) = DeterminePaymentMethod(transaction, eventCode);

                    var notification = new PaymentNotification
                    {
                        Date = dateTime.Date,
                        Time = dateTime.TimeOfDay,
                        TransactionId = transactionInfo.GetProperty("transaction_id").GetString()!,
                        Status = MapPayPalStatus(transactionInfo.GetProperty("transaction_status").GetString()!),
                        Amount = Math.Round(amountValue, 2), // Redondear a 2 decimales
                        PaymentMethod = paymentMethod,
                        Bank = bank
                    };

                    paymentNotifications.Add(notification);
                }
            }

            return paymentNotifications;
        }

        private string GetTransactionDescription(JsonElement transaction)
        {
            var description = new List<string>();

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

            return string.Join(" | ", description);
        }

        private static (string PaymentMethod, string Bank) DeterminePaymentMethod(JsonElement transaction, string? eventCode)
        {
            // Manejar específicamente el código T1900 (Balance Inicial)
            if (!string.IsNullOrEmpty(eventCode) && eventCode == "T1900")
            {
                return ("Initial Balance", "PayPal");
            }

            // Para otros códigos, usar la lógica existente
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

            return ("Unknown", "Unknown");
        }

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