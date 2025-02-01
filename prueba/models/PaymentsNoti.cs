using System;

namespace PaypalApi.Models
{
    public class PaymentNotification 
    {
        public int Id { get; set; }
        
        public required DateTime Date { get; set; }
        
        public required TimeSpan Time { get; set; }
        
        public required string TransactionId { get; set; }
        
        public required string Status { get; set; }
        
        public required decimal Amount { get; set; }
        
        public string Bank { get; set; } = string.Empty;
        
        public string PaymentMethod { get; set; } = string.Empty;
    }
}