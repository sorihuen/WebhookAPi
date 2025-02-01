using System.Collections.Generic;

namespace PaypalApi.Models
{
    public class PayPalResponse
    {
        public List<TransactionDetail>? TransactionDetails { get; set; }
    }

    public class TransactionDetail
    {
        public TransactionInfo? TransactionInfo { get; set; }
    }

    public class TransactionInfo
    {
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string TransactionInitiationDate { get; set; } = string.Empty;
        public TransactionAmount? TransactionAmount { get; set; }
        public string TransactionEventCode { get; set; } = string.Empty;
    }

    public class TransactionAmount
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

