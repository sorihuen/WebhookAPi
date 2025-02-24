using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaypalApi.Models
{
    public class PaymentNotification
    {
        public int Id { get; set; }

        public required DateTime Date { get; set; }

        public required TimeSpan Time { get; set; }

        public required string TransactionId { get; set; }

        public required string Status { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("amount")]
        [Column(TypeName = "decimal(18,2)")]

        public required decimal Amount { get; set; }

        public string Bank { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;
    }
}