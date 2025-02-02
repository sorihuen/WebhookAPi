using PaypalApi.Models;

namespace PaypalApi.Services
{
    public interface IPaymentProcessorService
    {
        List<PaymentNotification> ProcessPayPalTransactions(string jsonResponse);
    }
}