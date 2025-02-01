using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PaypalApi.Models;

public class PayPalDataProcessor
{
    public static List<PaymentNotification> ConvertToPaymentNotifications(string jsonResponse)
    {
        // Deserializar el JSON en el modelo PayPalResponse
        var payPalResponse = JsonSerializer.Deserialize<PayPalResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Validar que haya datos
        if (payPalResponse?.TransactionDetails == null)
        {
            return new List<PaymentNotification>();
        }

        // Convertir los datos recibidos a PaymentNotification
        return payPalResponse.TransactionDetails.Select(td => new PaymentNotification
        {
            Date = DateTime.TryParse(td.TransactionInfo?.TransactionInitiationDate, out var date) ? date.Date : DateTime.MinValue,
            Time = DateTime.TryParse(td.TransactionInfo?.TransactionInitiationDate, out var time) ? time.TimeOfDay : TimeSpan.Zero,
            TransactionId = td.TransactionInfo?.TransactionId ?? "Unknown",
            Status = td.TransactionInfo?.TransactionStatus ?? "Unknown",
            Amount = decimal.TryParse(td.TransactionInfo?.TransactionAmount?.Value, out var amount) ? amount : 0,
            Bank = "PayPal",
            // Aquí se incluye la validación de 'transaction_event_code'
            PaymentMethod = GetPaymentMethod(td.TransactionInfo)
        }).ToList();
    }

    // Método para determinar el PaymentMethod basado en 'transaction_event_code'
    private static string GetPaymentMethod(TransactionInfo? transactionInfo)
    {
        // Verificar que transactionInfo no sea nulo
        if (transactionInfo == null)
        {
            return "Unknown"; // Si transactionInfo es nulo, devolver "Unknown"
        }

        // Aquí puedes revisar la propiedad 'transaction_event_code'
        // El código de evento podría ser algo como "T1900", "T1901", etc.
        // Ejemplo de cómo determinar el tipo de pago según el código del evento
        switch (transactionInfo.TransactionEventCode)
        {
            case "T1900":
                return "PayPal";
            case "T1901":
                return "Credit Card";
            case "T1902":
                return "Bank Transfer";
            default:
                return "Unknown"; // Si el código no coincide con ninguna condición, devolver "Unknown"
        }
    }
}
