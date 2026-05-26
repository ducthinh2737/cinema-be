using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Payments;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreateVNPayPaymentUrlAsync(int bookingId, string ipAddress);
        Task<string> CreateMomoPaymentUrlAsync(int bookingId);
        Task<string> CreateZaloPayPaymentUrlAsync(int bookingId);
        Task<string> CreatePayPalPaymentUrlAsync(int bookingId);
        Task<bool> ProcessCallbackAsync(string gatewayName, Dictionary<string, string> callbackData);
        Task<bool> RefundPaymentAsync(int paymentId, string reason, decimal amount);
        Task<string> RetryPaymentUrlAsync(int bookingId, string gatewayName, string ipAddress);
    }
}
