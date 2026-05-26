using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.Helpers;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Logs;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly CinemaDbContext _context;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            CinemaDbContext context,
            IPaymentRepository paymentRepository,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _paymentRepository = paymentRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateVNPayPaymentUrlAsync(int bookingId, string ipAddress)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new ArgumentException("Booking not found.");
            if (booking.BookingStatus != "Pending") throw new InvalidOperationException("Booking is not in pending state.");

            // Create Payment record
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                PaymentMethod = "VNPay",
                PaymentStatus = "Pending",
                PaymentDate = DateTime.UtcNow,
                Amount = booking.TotalAmount
            };
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // Create Transaction record
            var transaction = new PaymentTransaction
            {
                PaymentId = payment.PaymentId,
                TransactionReference = booking.BookingCode,
                GatewayName = "VNPay",
                Amount = booking.TotalAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddTransactionAsync(transaction);
            await _paymentRepository.SaveChangesAsync();

            // Fetch Configurations or use fallbacks
            var vnpUrl = _configuration["VNPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var tmnCode = _configuration["VNPay:TmnCode"] ?? "CINEMA01";
            var hashSecret = _configuration["VNPay:HashSecret"] ?? "SECRET_HASH_VNPAY_12345";
            var returnUrl = _configuration["VNPay:ReturnUrl"] ?? "http://localhost:5156/api/payments/callback?gateway=vnpay";

            var paymentUrl = VNPayHelper.CreatePaymentUrl(
                vnpUrl,
                tmnCode,
                hashSecret,
                returnUrl,
                booking.BookingCode,
                booking.TotalAmount,
                $"Thanh toan ve xem phim cho ma booking: {booking.BookingCode}",
                ipAddress
            );

            await LogAuditAsync(booking.UserId, "CreateVNPayPaymentUrl", "Bookings", booking.BookingId);
            return paymentUrl;
        }

        public async Task<string> CreateMomoPaymentUrlAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new ArgumentException("Booking not found.");
            if (booking.BookingStatus != "Pending") throw new InvalidOperationException("Booking is not in pending state.");

            var payment = new Payment
            {
                BookingId = booking.BookingId,
                PaymentMethod = "Momo",
                PaymentStatus = "Pending",
                PaymentDate = DateTime.UtcNow,
                Amount = booking.TotalAmount
            };
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var transaction = new PaymentTransaction
            {
                PaymentId = payment.PaymentId,
                TransactionReference = booking.BookingCode,
                GatewayName = "Momo",
                Amount = booking.TotalAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddTransactionAsync(transaction);
            await _paymentRepository.SaveChangesAsync();

            // Configurations
            var partnerCode = _configuration["Momo:PartnerCode"] ?? "MOMO_CINEMA";
            var accessKey = _configuration["Momo:AccessKey"] ?? "ACCESS_KEY_MOMO";
            var secretKey = _configuration["Momo:SecretKey"] ?? "SECRET_KEY_MOMO";
            var ipnUrl = _configuration["Momo:IpnUrl"] ?? "http://localhost:5156/api/payments/callback?gateway=momo";
            var redirectUrl = _configuration["Momo:RedirectUrl"] ?? "http://localhost:5156/api/payments/callback?gateway=momo";

            var signature = MomoHelper.CreateSignature(
                partnerCode,
                accessKey,
                booking.BookingCode,
                ((long)booking.TotalAmount).ToString(),
                booking.BookingCode,
                $"Thanh toan Momo cho booking {booking.BookingCode}",
                redirectUrl,
                ipnUrl,
                "",
                "captureWallet",
                secretKey
            );

            // Mock Momo Payment Portal Redirect Url
            var momoPaymentUrl = $"https://test-payment.momo.vn/v2/gateway/api/create?partnerCode={partnerCode}&accessKey={accessKey}&requestId={booking.BookingCode}&amount={booking.TotalAmount}&orderId={booking.BookingCode}&signature={signature}";

            await LogAuditAsync(booking.UserId, "CreateMomoPaymentUrl", "Bookings", booking.BookingId);
            return momoPaymentUrl;
        }

        public async Task<string> CreateZaloPayPaymentUrlAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new ArgumentException("Booking not found.");
            if (booking.BookingStatus != "Pending") throw new InvalidOperationException("Booking is not pending.");

            var payment = new Payment
            {
                BookingId = booking.BookingId,
                PaymentMethod = "ZaloPay",
                PaymentStatus = "Pending",
                PaymentDate = DateTime.UtcNow,
                Amount = booking.TotalAmount
            };
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var transaction = new PaymentTransaction
            {
                PaymentId = payment.PaymentId,
                TransactionReference = booking.BookingCode,
                GatewayName = "ZaloPay",
                Amount = booking.TotalAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddTransactionAsync(transaction);
            await _paymentRepository.SaveChangesAsync();

            var appId = _configuration["ZaloPay:AppId"] ?? "2553";
            var key1 = _configuration["ZaloPay:Key1"] ?? "9phuKey1";

            var appTransId = DateTime.Now.ToString("yyMMdd") + "_" + booking.BookingCode;
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var signature = ZaloPayHelper.CreateSignature(appId, appTransId, "CinemaUser", ((long)booking.TotalAmount).ToString(), appTime, "{}", "[]", key1);

            var zalopayUrl = $"https://sb-openapi.zalopay.vn/v2/create?appid={appId}&apptransid={appTransId}&appuser=CinemaUser&amount={booking.TotalAmount}&apptime={appTime}&signature={signature}";

            await LogAuditAsync(booking.UserId, "CreateZaloPayPaymentUrl", "Bookings", booking.BookingId);
            return zalopayUrl;
        }

        public async Task<string> CreatePayPalPaymentUrlAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new ArgumentException("Booking not found.");
            if (booking.BookingStatus != "Pending") throw new InvalidOperationException("Booking is not pending.");

            var payment = new Payment
            {
                BookingId = booking.BookingId,
                PaymentMethod = "PayPal",
                PaymentStatus = "Pending",
                PaymentDate = DateTime.UtcNow,
                Amount = booking.TotalAmount
            };
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var transaction = new PaymentTransaction
            {
                PaymentId = payment.PaymentId,
                TransactionReference = booking.BookingCode,
                GatewayName = "PayPal",
                Amount = booking.TotalAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddTransactionAsync(transaction);
            await _paymentRepository.SaveChangesAsync();

            var clientId = _configuration["PayPal:ClientId"] ?? "PAYPAL_CLIENT_ID";
            var mode = _configuration["PayPal:Mode"] ?? "sandbox";
            var returnUrl = "http://localhost:5156/api/payments/callback?gateway=paypal";
            var cancelUrl = "http://localhost:5156/api/payments/callback?gateway=paypal&cancel=true";

            // Convert amount to USD (simulated conversion rate 25000 VND = 1 USD)
            var amountUsd = Math.Round(booking.TotalAmount / 25000m, 2);
            if (amountUsd <= 0) amountUsd = 1.00m;

            var approvalUrl = PayPalHelper.CreateApprovalUrl(clientId, mode, returnUrl, cancelUrl, booking.BookingCode, amountUsd);

            await LogAuditAsync(booking.UserId, "CreatePayPalPaymentUrl", "Bookings", booking.BookingId);
            return approvalUrl;
        }

        public async Task<bool> ProcessCallbackAsync(string gatewayName, Dictionary<string, string> callbackData)
        {
            var txnRef = "";
            var isSuccess = false;
            var responseCode = "";
            var message = "";

            if (gatewayName.Equals("vnpay", StringComparison.OrdinalIgnoreCase))
            {
                var hashSecret = _configuration["VNPay:HashSecret"] ?? "SECRET_HASH_VNPAY_12345";
                var secureHash = callbackData.ContainsKey("vnp_SecureHash") ? callbackData["vnp_SecureHash"] : "";
                
                if (!VNPayHelper.VerifySignature(callbackData, secureHash, hashSecret))
                {
                    _logger.LogWarning("VNPay signature verification failed.");
                    return false;
                }

                txnRef = callbackData.ContainsKey("vnp_TxnRef") ? callbackData["vnp_TxnRef"] : "";
                responseCode = callbackData.ContainsKey("vnp_ResponseCode") ? callbackData["vnp_ResponseCode"] : "";
                isSuccess = responseCode == "00";
                message = isSuccess ? "VNPay Success" : "VNPay Failed with code " + responseCode;
            }
            else if (gatewayName.Equals("momo", StringComparison.OrdinalIgnoreCase))
            {
                var secretKey = _configuration["Momo:SecretKey"] ?? "SECRET_KEY_MOMO";
                var signature = callbackData.ContainsKey("signature") ? callbackData["signature"] : "";
                
                var partnerCode = callbackData.GetValueOrDefault("partnerCode", "");
                var orderId = callbackData.GetValueOrDefault("orderId", "");
                var requestId = callbackData.GetValueOrDefault("requestId", "");
                var amount = callbackData.GetValueOrDefault("amount", "");
                var msg = callbackData.GetValueOrDefault("message", "");
                var resultCode = callbackData.GetValueOrDefault("resultCode", "");
                var transId = callbackData.GetValueOrDefault("transId", "");
                var extraData = callbackData.GetValueOrDefault("extraData", "");

                if (!MomoHelper.VerifySignature(partnerCode, orderId, requestId, amount, msg, resultCode, transId, extraData, signature, secretKey))
                {
                    _logger.LogWarning("Momo signature verification failed.");
                    return false;
                }

                txnRef = orderId;
                responseCode = resultCode;
                isSuccess = resultCode == "0";
                message = msg;
            }
            else
            {
                // General or simulated callbacks (PayPal/ZaloPay)
                txnRef = callbackData.GetValueOrDefault("txnRef", "");
                responseCode = callbackData.GetValueOrDefault("status", "00");
                isSuccess = responseCode == "00" || responseCode == "success";
                message = "Simulated payment callback";
            }

            var transaction = await _paymentRepository.GetTransactionByRefAsync(txnRef);
            if (transaction == null)
            {
                _logger.LogWarning($"Transaction reference {txnRef} not found in database.");
                return false;
            }

            if (transaction.Status != "Pending")
            {
                _logger.LogInformation($"Transaction {txnRef} has already been processed with status {transaction.Status}.");
                return true;
            }

            // Update Transaction and Payment status
            transaction.Status = isSuccess ? "Success" : "Failed";
            transaction.ResponseCode = responseCode;
            transaction.ResponseMessage = message;
            _paymentRepository.UpdateTransaction(transaction);

            var payment = await _paymentRepository.GetByIdAsync(transaction.PaymentId);
            if (payment != null)
            {
                payment.PaymentStatus = isSuccess ? "Completed" : "Failed";
                _paymentRepository.Update(payment);

                // Update Booking status
                var booking = await _context.Bookings.FindAsync(payment.BookingId);
                if (booking != null)
                {
                    if (isSuccess)
                    {
                        booking.BookingStatus = "Confirmed";
                        booking.QRCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={booking.BookingCode}";
                    }
                    else
                    {
                        booking.BookingStatus = "Cancelled";
                    }
                    _context.Bookings.Update(booking);
                }
            }

            await _paymentRepository.SaveChangesAsync();
            await LogAuditAsync(null, "ProcessCallback_" + gatewayName, "Payments", transaction.PaymentId);
            return isSuccess;
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, string reason, decimal amount)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null) throw new ArgumentException("Payment not found.");
            if (payment.PaymentStatus != "Completed") throw new InvalidOperationException("Only completed payments can be refunded.");
            if (amount > payment.Amount) throw new ArgumentException("Refund amount cannot exceed original payment amount.");

            var refund = new Refund
            {
                PaymentId = payment.PaymentId,
                RefundReason = reason,
                RefundAmount = amount,
                RefundStatus = "Success",
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddRefundAsync(refund);

            payment.PaymentStatus = "Refunded";
            _paymentRepository.Update(payment);

            var booking = await _context.Bookings.FindAsync(payment.BookingId);
            if (booking != null)
            {
                booking.BookingStatus = "Cancelled";
                _context.Bookings.Update(booking);
            }

            await _paymentRepository.SaveChangesAsync();
            await LogAuditAsync(booking?.UserId, "RefundPayment", "Payments", paymentId);
            return true;
        }

        public async Task<string> RetryPaymentUrlAsync(int bookingId, string gatewayName, string ipAddress)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new ArgumentException("Booking not found.");
            if (booking.BookingStatus == "Confirmed") throw new InvalidOperationException("Booking is already confirmed.");

            // Cancel any old pending payments for this booking
            var oldPayments = await _context.Payments
                .Where(p => p.BookingId == bookingId && p.PaymentStatus == "Pending")
                .ToListAsync();

            foreach (var p in oldPayments)
            {
                p.PaymentStatus = "Cancelled";
                _paymentRepository.Update(p);
            }
            await _paymentRepository.SaveChangesAsync();

            // Set booking back to Pending if it was Cancelled due to payment failure
            if (booking.BookingStatus == "Cancelled")
            {
                booking.BookingStatus = "Pending";
                booking.CreatedAt = DateTime.UtcNow; // Reset checkout timer
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
            }

            return gatewayName.ToLower() switch
            {
                "vnpay" => await CreateVNPayPaymentUrlAsync(bookingId, ipAddress),
                "momo" => await CreateMomoPaymentUrlAsync(bookingId),
                "zalopay" => await CreateZaloPayPaymentUrlAsync(bookingId),
                "paypal" => await CreatePayPalPaymentUrlAsync(bookingId),
                _ => throw new ArgumentException("Unsupported payment gateway.")
            };
        }

        private async Task LogAuditAsync(int? userId, string action, string tableName, int recordId)
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
    }
}
