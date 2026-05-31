using System;
using System.Threading.Tasks;

namespace CinemaBooking.API.Services.Interfaces
{
    public class VietQRPaymentResponseDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public string QrImageUrl { get; set; } = null!;
        public string TransferContent { get; set; } = null!;
        public string BankName { get; set; } = null!;
        public string AccountNo { get; set; } = null!;
        public string AccountName { get; set; } = null!;
    }

    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public DateTime PaymentDate { get; set; }
    }

    public class ConfirmPaymentDto
    {
        public int BookingId { get; set; }
        public string? AdminNotes { get; set; }
    }

    /// <summary>
    /// Service for handling VietQR payments, expirations, and confirmation workflows.
    /// </summary>
    public interface IPaymentService
    {
        Task<VietQRPaymentResponseDto> CreateVietQRPaymentAsync(int bookingId);
        Task<PaymentResponseDto> ConfirmPaymentAsync(int bookingId);
        Task ExpirePendingPaymentsAsync();

        // Backward compatibility signatures for other controllers
        Task<string> CreateVietQRPaymentUrlAsync(int bookingId);
        Task<string> CreateVNPayPaymentUrlAsync(int bookingId, string ipAddress);
        Task<string> RetryPaymentUrlAsync(int bookingId, string gatewayName, string ipAddress);
    }
}
