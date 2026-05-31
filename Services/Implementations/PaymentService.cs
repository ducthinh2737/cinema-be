using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Logs;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Production-ready VietQR-only payment processor.
    /// Manages the creation, verification, expiration, and logging of bank transfer QR codes.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly CinemaDbContext _context;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PaymentService(
            CinemaDbContext context,
            IPaymentRepository paymentRepository,
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _paymentRepository = paymentRepository;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #region Public VietQR Methods

        /// <summary>
        /// Generates a VietQR transfer details payload and records pending payment state.
        /// </summary>
        public async Task<VietQRPaymentResponseDto> CreateVietQRPaymentAsync(int bookingId)
        {
            _logger.LogInformation("Creating VietQR payment for Booking ID {BookingId}", bookingId);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            
            if (booking == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy vé đặt có mã ID {bookingId}.");
            }

            if (booking.BookingStatus != "Pending")
            {
                throw new InvalidOperationException($"Giao dịch đặt vé đang ở trạng thái {booking.BookingStatus}. Chỉ vé Pending mới có thể thanh toán.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create or find Payment record
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = booking.TotalAmount,
                        PaymentMethod = "VietQR",
                        PaymentStatus = "Pending",
                        PaymentDate = DateTime.UtcNow
                    };
                    await _context.Payments.AddAsync(payment);
                }
                else
                {
                    payment.PaymentMethod = "VietQR";
                    payment.PaymentStatus = "Pending";
                    payment.PaymentDate = DateTime.UtcNow;
                    _context.Payments.Update(payment);
                }
                await _context.SaveChangesAsync();

                // Create Payment Transaction log
                var paymentTransaction = new PaymentTransaction
                {
                    PaymentId = payment.PaymentId,
                    TransactionReference = booking.BookingCode,
                    GatewayName = "VietQR",
                    Amount = booking.TotalAmount,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.PaymentTransactions.AddAsync(paymentTransaction);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Retrieve bank configurations from AppSettings
                var bankId = _configuration["VietQR:BankId"] ?? "MB";
                var accountNo = _configuration["VietQR:AccountNo"] ?? "0382300380";
                var accountName = _configuration["VietQR:AccountName"] ?? "CP RAP CHIEU PHIM CINEMA VIETNAM";

                // Form VietQR compact link
                var qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={Convert.ToInt64(booking.TotalAmount)}&addInfo={booking.BookingCode}";

                _logger.LogInformation("VietQR Payment generated successfully for Booking Code {BookingCode}", booking.BookingCode);
                await LogAuditAsync(booking.UserId, "CreateVietQRPayment", "Payments", payment.PaymentId);

                return new VietQRPaymentResponseDto
                {
                    BookingId = bookingId,
                    BookingCode = booking.BookingCode,
                    Amount = booking.TotalAmount,
                    QrImageUrl = qrUrl,
                    TransferContent = booking.BookingCode,
                    BankName = bankId,
                    AccountNo = accountNo,
                    AccountName = accountName
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error generating VietQR Payment for Booking ID {BookingId}", bookingId);
                throw;
            }
        }

        /// <summary>
        /// Confirms a VietQR bank transfer, confirming booking, generating ticket, and releasing locks.
        /// </summary>
        public async Task<PaymentResponseDto> ConfirmPaymentAsync(int bookingId)
        {
            _logger.LogInformation("Confirming payment bank transfer for Booking ID {BookingId}", bookingId);

            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy vé đặt có mã ID {bookingId}.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                booking.BookingStatus = "Confirmed";
                booking.QRCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={booking.BookingCode}";
                _context.Bookings.Update(booking);

                // Update or create payment
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = booking.TotalAmount,
                        PaymentMethod = "VietQR",
                        PaymentStatus = "Paid",
                        PaymentDate = DateTime.UtcNow
                    };
                    await _context.Payments.AddAsync(payment);
                }
                else
                {
                    payment.PaymentStatus = "Paid";
                    payment.PaymentDate = DateTime.UtcNow;
                    _context.Payments.Update(payment);
                }
                await _context.SaveChangesAsync();

                // Complete transactions
                var txs = await _context.PaymentTransactions
                    .Where(t => t.PaymentId == payment.PaymentId)
                    .ToListAsync();

                foreach (var tx in txs)
                {
                    tx.Status = "Paid";
                    tx.ResponseCode = "00";
                    tx.ResponseMessage = "Paid";
                    _context.PaymentTransactions.Update(tx);
                }
                await _context.SaveChangesAsync();

                // Increment promo usage
                var promoLink = await _context.BookingPromotions.FirstOrDefaultAsync(bp => bp.BookingId == booking.BookingId);
                if (promoLink != null)
                {
                    var promo = await _context.Promotions.FindAsync(promoLink.PromotionId);
                    if (promo != null)
                    {
                        promo.CurrentUsage++;
                        _context.Promotions.Update(promo);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation("VietQR Payment confirmed successfully. Booking Code: {BookingCode}", booking.BookingCode);
                await LogAuditAsync(booking.UserId, "ConfirmPayment", "Payments", payment.PaymentId);

                // Unlock seats on in-memory distributed cache and broadcast
                var seatLockService = _serviceProvider.GetService(typeof(ISeatLockService)) as ISeatLockService;
                if (seatLockService != null)
                {
                    var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                    await seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, seatIds, booking.UserId.ToString(), string.Empty, force: true);

                    var hubContext = _serviceProvider.GetService(typeof(IHubContext<SeatHub>)) as IHubContext<SeatHub>;
                    if (hubContext != null)
                    {
                        await hubContext.Clients.All.SendAsync("BookingConfirmed", booking.ShowtimeId, seatIds);
                    }
                }

                return new PaymentResponseDto
                {
                    PaymentId = payment.PaymentId,
                    BookingId = bookingId,
                    Amount = payment.Amount,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentDate = payment.PaymentDate
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to confirm bank transfer VietQR for Booking ID {BookingId}", bookingId);
                throw;
            }
        }

        /// <summary>
        /// Sweeps expired VietQR payments and auto-releases locked seats.
        /// </summary>
        public async Task ExpirePendingPaymentsAsync()
        {
            var threshold = DateTime.UtcNow.AddMinutes(-5);
            _logger.LogInformation("Running expired VietQR payments sweep for entries created before {Threshold}", threshold);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var expiredPayments = await _context.Payments
                    .Include(p => p.Booking).ThenInclude(b => b.BookingSeats)
                    .Where(p => p.PaymentStatus == "Pending" && p.PaymentDate < threshold)
                    .ToListAsync();

                if (!expiredPayments.Any()) return;

                var seatLockService = _serviceProvider.GetService(typeof(ISeatLockService)) as ISeatLockService;
                var hubContext = _serviceProvider.GetService(typeof(IHubContext<SeatHub>)) as IHubContext<SeatHub>;

                foreach (var payment in expiredPayments)
                {
                    payment.PaymentStatus = "Expired";
                    _context.Payments.Update(payment);

                    if (payment.Booking != null && payment.Booking.BookingStatus == "Pending")
                    {
                        payment.Booking.BookingStatus = "Expired";
                        _context.Bookings.Update(payment.Booking);

                        if (seatLockService != null)
                        {
                            var seatIds = payment.Booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                            await seatLockService.UnlockMultipleSeatsAsync(payment.Booking.ShowtimeId, seatIds, payment.Booking.UserId.ToString(), string.Empty, force: true);

                            if (hubContext != null)
                            {
                                foreach (var seatId in seatIds)
                                {
                                    await hubContext.Clients.All.SendAsync("SeatReleased", payment.Booking.ShowtimeId, seatId);
                                }
                            }
                        }
                    }

                    // Expire Transactions
                    var txs = await _context.PaymentTransactions.Where(t => t.PaymentId == payment.PaymentId).ToListAsync();
                    foreach (var tx in txs)
                    {
                        tx.Status = "Expired";
                        _context.PaymentTransactions.Update(tx);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully expired {Count} pending payments.", expiredPayments.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error sweeping expired VietQR payments.");
                throw;
            }
        }

        #endregion

        #region Backward Compatibility Interfaces

        public async Task<string> CreateVietQRPaymentUrlAsync(int bookingId)
        {
            var res = await CreateVietQRPaymentAsync(bookingId);
            return res.QrImageUrl;
        }

        public async Task<string> CreateVNPayPaymentUrlAsync(int bookingId, string ipAddress)
        {
            var res = await CreateVietQRPaymentAsync(bookingId);
            return res.QrImageUrl;
        }

        public async Task<string> RetryPaymentUrlAsync(int bookingId, string gatewayName, string ipAddress)
        {
            var res = await CreateVietQRPaymentAsync(bookingId);
            return res.QrImageUrl;
        }

        #endregion

        #region Private Audit Helper

        private async Task LogAuditAsync(int? userId, string action, string tableName, int recordId)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    CreatedAt = DateTime.UtcNow
                };
                await _paymentRepository.AddAuditLogAsync(log);
                await _paymentRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log entry.");
            }
        }

        #endregion
    }
}
