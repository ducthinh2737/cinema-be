using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QRCoder;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Bookings;
using CinemaBooking.API.Models.Bookings;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Production-grade Booking Service managing ticket purchases, concurrent seating locking, and payments.
    /// Consolidated in a single file for deployment and maintenance simplicity.
    /// </summary>
    public class BookingService : IBookingService
    {
        #region Dependencies

        private readonly CinemaDbContext _context;
        private readonly IBookingRepository _bookingRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly ISeatLockService _seatLockService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<BookingService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BookingService(
            CinemaDbContext context,
            IBookingRepository bookingRepository,
            IMapper mapper,
            IHubContext<SeatHub> hubContext,
            ISeatLockService seatLockService,
            IMemoryCache memoryCache,
            ILogger<BookingService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _bookingRepository = bookingRepository;
            _mapper = mapper;
            _hubContext = hubContext;
            _seatLockService = seatLockService;
            _memoryCache = memoryCache;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves a booking by its ID with full details.
        /// </summary>
        public async Task<ApiResponse<BookingResponseDto>> GetBookingByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving booking by ID: {BookingId}", id);
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == id, cancellationToken);

            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {id} not found.");
            }

            var dto = _mapper.Map<BookingDto>(booking);
            return ApiResponse.Success(MapToResponseDto(dto, booking.QRCodeUrl));
        }

        /// <summary>
        /// Retrieves all bookings belonging to a specific user.
        /// </summary>
        public async Task<ApiResponse<IEnumerable<BookingResponseDto>>> GetUserBookingsAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving bookings for User ID: {UserId}", userId);
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var mappedList = bookings.Select(b => {
                var dto = _mapper.Map<BookingDto>(b);
                return MapToResponseDto(dto, b.QRCodeUrl);
            });

            return ApiResponse.Success(mappedList);
        }

        /// <summary>
        /// Retrieves all bookings in the system (Admin only).
        /// </summary>
        public async Task<ApiResponse<IEnumerable<BookingResponseDto>>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving all bookings for Admin");
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .OrderByDescending(b => b.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var mappedList = bookings.Select(b => {
                var dto = _mapper.Map<BookingDto>(b);
                return MapToResponseDto(dto, b.QRCodeUrl);
            });

            return ApiResponse.Success(mappedList);
        }


        /// <summary>
        /// Creates a new pending booking with concurrency seat reservation checks.
        /// Uses Serializable transaction level and double-check database validation to prevent race conditions.
        /// </summary>
        public async Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(int userId, BookingCreateDto createDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to create booking for User ID {UserId} on Showtime ID {ShowtimeId}", userId, createDto.ShowtimeId);
            
            // 1. Validation
            if (createDto.SeatIds == null || !createDto.SeatIds.Any())
            {
                throw new ValidationException("Bạn phải chọn ít nhất một ghế để đặt.");
            }

            // 2. Validate Showtime exists and is active
            var showtime = await _context.Showtimes
                .Include(s => s.Price)
                .Include(s => s.Hall).ThenInclude(h => h.Cinema)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == createDto.ShowtimeId, cancellationToken);

            if (showtime == null)
            {
                throw new NotFoundException($"Suất chiếu {createDto.ShowtimeId} không tồn tại.");
            }

            if (showtime.StartTime < DateTime.UtcNow)
            {
                throw new BusinessException("Không thể đặt vé cho suất chiếu đã bắt đầu hoặc đã qua.");
            }

            var cacheLockAcquired = await _seatLockService.LockMultipleSeatsAsync(
                createDto.ShowtimeId, 
                createDto.SeatIds, 
                userId.ToString(), 
                createDto.SessionId ?? string.Empty,
                minutes: 5,
                cancellationToken
            );

            if (cacheLockAcquired == null || !cacheLockAcquired.Success)
            {
                throw new BusinessException("Một hoặc nhiều ghế bạn chọn đã bị khóa bởi người dùng khác. Vui lòng chọn ghế khác.");
            }

            // 4. Database Transaction for safe serialization checks to prevent double-booking
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                // Double-check seat availability in database
                var seatAvailability = await ValidateSeatsAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString(), cancellationToken);
                if (!seatAvailability.IsSuccess)
                {
                    throw new BusinessException(seatAvailability.Message);
                }

                // Load seats metadata
                var seats = await _context.Seats
                    .Include(s => s.SeatType)
                    .Where(s => createDto.SeatIds.Contains(s.SeatId))
                    .ToListAsync(cancellationToken);

                if (seats.Count != createDto.SeatIds.Count)
                {
                    throw new ValidationException("Một hoặc nhiều mã ghế chọn không hợp lệ.");
                }

                // Calculate amounts
                var totalCalculation = await CalculateTotalAmountAsync(createDto.ShowtimeId, createDto.SeatIds, createDto.PromoCode, cancellationToken);
                if (!totalCalculation.IsSuccess)
                {
                    throw new BusinessException(totalCalculation.Message);
                }

                // Calculate subtotal for discount checks
                decimal subtotal = 0;
                var bookingSeatsList = new List<BookingSeat>();
                foreach (var seat in seats)
                {
                    decimal multiplier = seat.SeatType?.PriceMultiplier ?? 1.0m;
                    decimal seatPrice = showtime.Price.Value * multiplier;
                    subtotal += seatPrice;

                    bookingSeatsList.Add(new BookingSeat
                    {
                        SeatId = seat.SeatId,
                        UnitPrice = seatPrice
                    });
                }

                decimal serviceFee = seats.Any() ? 5000m : 0m;
                decimal discountAmount = Math.Max(0, (subtotal + serviceFee) - totalCalculation.Data);

                // Generate booking code
                var bookingCodeResponse = await GenerateBookingCodeAsync(cancellationToken);
                var bookingCode = bookingCodeResponse.Data;

                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    UserId = userId,
                    ShowtimeId = createDto.ShowtimeId,
                    TotalAmount = totalCalculation.Data,
                    ServiceFee = serviceFee,
                    DiscountAmount = discountAmount,
                    BookingStatus = BookingStatus.Pending.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    BookingSeats = bookingSeatsList
                };

                await _context.Bookings.AddAsync(booking, cancellationToken);
                
                // Save Booking first to generate the database-assigned BookingId identity value
                await _context.SaveChangesAsync(cancellationToken);
                
                // If promo applied, link it
                if (!string.IsNullOrEmpty(createDto.PromoCode))
                {
                    var promotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.PromoCode == createDto.PromoCode && p.IsActive && !p.IsDeleted, cancellationToken);

                    if (promotion != null)
                    {
                        var bookingPromo = new BookingPromotion
                        {
                            BookingId = booking.BookingId,
                            PromotionId = promotion.PromotionId
                        };
                        await _context.BookingPromotions.AddAsync(bookingPromo, cancellationToken);
                    }
                }

                // Add Pending payment record using the generated BookingId
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = booking.TotalAmount,
                    PaymentMethod = PaymentMethod.VietQR.ToString(),
                    PaymentStatus = PaymentStatus.Pending.ToString(),
                    PaymentDate = DateTime.UtcNow
                };
                await _context.Payments.AddAsync(payment, cancellationToken);
                
                // Save associated promotion links and payment records
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation("Booking created successfully. Code: {BookingCode}, ID: {BookingId}", bookingCode, booking.BookingId);

                // Broadcast SeatSelected realtime events via optimized SignalR helper
                await BroadcastSeatLockedAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString());

                // Resolve Payment URL dynamically if available
                var paymentService = _serviceProvider.GetService(typeof(IPaymentService)) as IPaymentService;
                string? paymentUrl = null;
                if (paymentService != null)
                {
                    try
                    {
                        paymentUrl = await paymentService.CreateVietQRPaymentUrlAsync(booking.BookingId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to dynamically resolve VietQR payment URL.");
                    }
                }

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    paymentUrl = $"/api/payments/retry?bookingId={booking.BookingId}&gateway=VietQR";
                }

                // Fetch final details
                var savedBooking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                    .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId, cancellationToken);

                var finalDto = _mapper.Map<BookingDto>(savedBooking ?? booking);
                var response = MapToResponseDto(finalDto, savedBooking?.QRCodeUrl);
                response.PaymentUrl = paymentUrl;

                return ApiResponse.Success(response, "Đặt vé tạm thời thành công. Vui lòng thanh toán trong vòng 5 phút.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                await _seatLockService.UnlockMultipleSeatsAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString(), createDto.SessionId ?? string.Empty);
                _logger.LogError(ex, "Error occurred during booking creation. Rolling back transaction and releasing locks.");
                throw;
            }
        }

        /// <summary>
        /// Backward compatible confirmation endpoint. Calls ConfirmPaymentAsync internally.
        /// </summary>
        public async Task<ApiResponse<BookingResponseDto>> ConfirmBookingAsync(BookingConfirmDto confirmDto, CancellationToken cancellationToken = default)
        {
            return await ConfirmPaymentAsync(confirmDto.BookingId, confirmDto.PaymentMethod, cancellationToken);
        }

        /// <summary>
        /// Cancels a booking, releases database holds, and releases cache locks.
        /// </summary>
        public async Task<ApiResponse<bool>> CancelBookingAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cancelling Booking ID: {BookingId}", bookingId);
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingSeats)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

                if (booking == null)
                {
                    throw new NotFoundException($"Booking with ID {bookingId} not found.");
                }

                if (booking.BookingStatus == BookingStatus.Cancelled.ToString())
                {
                    return ApiResponse.Success(true, "Vé đã ở trạng thái hủy trước đó.");
                }

                booking.BookingStatus = BookingStatus.Cancelled.ToString();
                _context.Bookings.Update(booking);

                // Cancel payments as well
                var payments = await _context.Payments.Where(p => p.BookingId == bookingId).ToListAsync(cancellationToken);
                foreach (var payment in payments)
                {
                    payment.PaymentStatus = PaymentStatus.Cancelled.ToString();
                    _context.Payments.Update(payment);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Unlock seats and notify SignalR clients
                var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, seatIds, booking.UserId.ToString(), string.Empty, force: true);
                await BroadcastSeatReleasedAsync(booking.ShowtimeId, seatIds);

                return ApiResponse.Success(true, "Hủy đặt vé thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred during cancellation of Booking ID {BookingId}", bookingId);
                throw;
            }
        }

        /// <summary>
        /// Releases all expired pending bookings that exceeded the 5-minute threshold.
        /// </summary>
        public async Task ReleaseExpiredBookingsAsync(CancellationToken cancellationToken = default)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-5);
            _logger.LogInformation("Scanning for expired pending bookings created before {Threshold}", threshold);

            // Fetch expired bookings
            var expiredBookings = await _context.Bookings
                .Include(b => b.BookingSeats)
                .Where(b => b.BookingStatus == BookingStatus.Pending.ToString() && b.CreatedAt < threshold)
                .ToListAsync(cancellationToken);

            if (!expiredBookings.Any()) return;

            foreach (var booking in expiredBookings)
            {
                _logger.LogInformation("Expiring Booking Code {BookingCode} (ID: {BookingId})", booking.BookingCode, booking.BookingId);
                booking.BookingStatus = BookingStatus.Expired.ToString();
                _context.Bookings.Update(booking);

                // Cancel payments
                var payments = await _context.Payments.Where(p => p.BookingId == booking.BookingId).ToListAsync(cancellationToken);
                foreach (var p in payments)
                {
                    p.PaymentStatus = PaymentStatus.Cancelled.ToString();
                    _context.Payments.Update(p);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Release cache locks and notify SignalR clients
            foreach (var booking in expiredBookings)
            {
                var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, seatIds, booking.UserId.ToString(), string.Empty, force: true);
                await BroadcastSeatReleasedAsync(booking.ShowtimeId, seatIds);
            }
        }

        /// <summary>
        /// Wrapper mapping for old background service calls.
        /// </summary>
        public async Task ExpirePendingBookingsAsync(CancellationToken cancellationToken = default)
        {
            await ReleaseExpiredBookingsAsync(cancellationToken);
        }

        #endregion

        #region Booking Validation

        /// <summary>
        /// Validates seats database booking availability.
        /// </summary>
        public async Task<ApiResponse<bool>> ValidateSeatsAsync(int showtimeId, List<int> seatIds, string userId, CancellationToken cancellationToken = default)
        {
            if (seatIds == null || !seatIds.Any())
            {
                return ApiResponse.Fail<bool>("Danh sách vị trí ghế trống.");
            }

            // Lock hold threshold is 5 minutes
            var holdThreshold = DateTime.UtcNow.AddMinutes(-5);

            var bookedSeatIds = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.ShowtimeId == showtimeId &&
                             bs.Booking.BookingStatus != BookingStatus.Cancelled.ToString() &&
                             (bs.Booking.BookingStatus == BookingStatus.Confirmed.ToString() || 
                              bs.Booking.BookingStatus == "Paid" || 
                              bs.Booking.CreatedAt > holdThreshold))
                .Select(bs => bs.SeatId)
                .ToListAsync(cancellationToken);

            var doubleBooked = seatIds.Intersect(bookedSeatIds).ToList();
            if (doubleBooked.Any())
            {
                return ApiResponse.Fail<bool>($"Ghế số {string.Join(", ", doubleBooked)} đã bị khách hàng khác đặt hoặc đang giữ.");
            }

            return ApiResponse.Success(true);
        }

        #endregion

        #region Booking Pricing

        /// <summary>
        /// Calculates the final booking price after discounts, applying service fees.
        /// </summary>
        public async Task<ApiResponse<decimal>> CalculateTotalAmountAsync(int showtimeId, List<int> seatIds, string? promoCode, CancellationToken cancellationToken = default)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Price)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken);

            if (showtime == null)
            {
                return ApiResponse.Fail<decimal>($"Suất chiếu ID {showtimeId} không tồn tại.");
            }

            var seats = await _context.Seats
                .Include(s => s.SeatType)
                .Where(s => seatIds.Contains(s.SeatId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (seats.Count != seatIds.Count)
            {
                return ApiResponse.Fail<decimal>("Một số ID ghế được chọn không hợp lệ.");
            }

            decimal subtotal = 0;
            foreach (var seat in seats)
            {
                decimal multiplier = seat.SeatType?.PriceMultiplier ?? 1.0m;
                subtotal += showtime.Price.Value * multiplier;
            }

            decimal serviceFee = seatIds.Any() ? 5000m : 0m;
            decimal discount = 0;

            if (!string.IsNullOrEmpty(promoCode))
            {
                var promotion = await _context.Promotions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive && !p.IsDeleted, cancellationToken);

                if (promotion != null)
                {
                    var now = DateTime.UtcNow;
                    if (now >= promotion.StartDate && now <= promotion.EndDate && promotion.CurrentUsage < promotion.MaxUsage)
                    {
                        if (promotion.DiscountType == "Percentage" || promotion.DiscountValue <= 1.0m)
                        {
                            discount = subtotal * promotion.DiscountValue;
                        }
                        else
                        {
                            discount = promotion.DiscountValue;
                        }
                    }
                }
            }

            decimal totalAmount = Math.Max(0, subtotal + serviceFee - discount);
            return ApiResponse.Success(totalAmount);
        }

        #endregion

        #region Seat Locking

        // Handled via injected ISeatLockService. Reserved for extension lock logic if needed.

        #endregion

        #region Payment Processing

        /// <summary>
        /// Confirms booking and payment status, generating QR ticket and releasing cache locks.
        /// </summary>
        public async Task<ApiResponse<BookingResponseDto>> ConfirmPaymentAsync(int bookingId, string paymentMethod, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing payment confirmation for Booking ID: {BookingId} via {PaymentMethod}", bookingId, paymentMethod);
            
            // Accept only VietQR or Cash
            if (paymentMethod != PaymentMethod.VietQR.ToString() && paymentMethod != PaymentMethod.Cash.ToString())
            {
                throw new ValidationException("Chỉ chấp nhận thanh toán qua VietQR hoặc Tiền mặt (Cash).");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

                if (booking == null)
                {
                    throw new NotFoundException($"Booking with ID {bookingId} not found.");
                }

                if (booking.BookingStatus == BookingStatus.Confirmed.ToString() || booking.BookingStatus == "Paid")
                {
                    var existingDto = _mapper.Map<BookingDto>(booking);
                    return ApiResponse.Success(MapToResponseDto(existingDto, booking.QRCodeUrl), "Thanh toán đã được xác nhận trước đó.");
                }

                if (booking.BookingStatus == BookingStatus.Cancelled.ToString() || booking.BookingStatus == BookingStatus.Expired.ToString())
                {
                    throw new BusinessException("Không thể xác nhận thanh toán cho vé đã hủy hoặc hết hạn.");
                }

                // Validate 5-minute timeout hold limit
                if (booking.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
                {
                    booking.BookingStatus = BookingStatus.Cancelled.ToString();
                    _context.Bookings.Update(booking);

                    var paymentsCancel = await _context.Payments.Where(p => p.BookingId == booking.BookingId).ToListAsync(cancellationToken);
                    foreach (var p in paymentsCancel)
                    {
                        p.PaymentStatus = PaymentStatus.Cancelled.ToString();
                        _context.Payments.Update(p);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Release seats and notify clients
                    var expiredSeatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                    await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, expiredSeatIds, booking.UserId.ToString(), string.Empty, force: true);
                    await BroadcastSeatReleasedAsync(booking.ShowtimeId, expiredSeatIds);

                    throw new BusinessException("Giao dịch đặt vé đã hết thời hạn thanh toán (5 phút). Ghế đã được giải phóng.");
                }

                // Confirm booking status & generate local base64 QR Code
                booking.BookingStatus = BookingStatus.Confirmed.ToString();
                booking.QRCodeUrl = GenerateBase64QRCode(booking.BookingCode);

                // Record payment completion
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.BookingId, cancellationToken);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        BookingId = booking.BookingId,
                        Amount = booking.TotalAmount,
                        PaymentMethod = paymentMethod,
                        PaymentStatus = PaymentStatus.Success.ToString(),
                        PaymentDate = DateTime.UtcNow
                    };
                    await _context.Payments.AddAsync(payment, cancellationToken);
                }
                else
                {
                    payment.PaymentMethod = paymentMethod;
                    payment.PaymentStatus = PaymentStatus.Success.ToString();
                    payment.PaymentDate = DateTime.UtcNow;
                    _context.Payments.Update(payment);
                }

                // Update Promotion usages
                var promoLink = await _context.BookingPromotions.FirstOrDefaultAsync(bp => bp.BookingId == booking.BookingId, cancellationToken);
                if (promoLink != null)
                {
                    var promo = await _context.Promotions.FindAsync(new object[] { promoLink.PromotionId }, cancellationToken);
                    if (promo != null)
                    {
                        promo.CurrentUsage++;
                        _context.Promotions.Update(promo);
                    }
                }

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Payment confirmed successfully for Booking ID: {BookingId}", bookingId);

                // Release seats from cache lock and broadcast BookingConfirmed
                var confirmedSeatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, confirmedSeatIds, booking.UserId.ToString(), string.Empty, force: true);
                await BroadcastBookingConfirmedAsync(booking.ShowtimeId, confirmedSeatIds);

                var finalDto = _mapper.Map<BookingDto>(booking);
                return ApiResponse.Success(MapToResponseDto(finalDto, booking.QRCodeUrl), "Thanh toán thành công. Vé đã được xác nhận.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error verifying payment for Booking ID: {BookingId}", bookingId);
                throw;
            }
        }

        #endregion

        #region Booking Code Generator

        /// <summary>
        /// Generates a unique booking code in the format: BK-yyyyMMdd-XXXXXX where XXXXXX is a random hex string.
        /// </summary>
        public Task<ApiResponse<string>> GenerateBookingCodeAsync(CancellationToken cancellationToken = default)
        {
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomHex = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            var code = $"BK-{dateStr}-{randomHex}";
            return Task.FromResult(ApiResponse.Success(code));
        }

        #endregion

        #region SignalR Broadcasting

        /// <summary>
        /// Broadcasts seat lock events to the specific showtime SignalR groups.
        /// </summary>
        public async Task BroadcastSeatLockedAsync(int showtimeId, List<int> seatIds, string userId)
        {
            try
            {
                foreach (var seatId in seatIds)
                {
                    await _hubContext.Clients.Group($"Showtime_{showtimeId}").SendAsync("SeatSelected", showtimeId, seatId, userId);
                    await _hubContext.Clients.Group($"showtime-{showtimeId}").SendAsync("SeatSelected", showtimeId, seatId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast SeatSelected event to showtime group {ShowtimeId}", showtimeId);
            }
        }

        /// <summary>
        /// Broadcasts seat release events to the specific showtime SignalR groups.
        /// </summary>
        public async Task BroadcastSeatReleasedAsync(int showtimeId, List<int> seatIds)
        {
            try
            {
                foreach (var seatId in seatIds)
                {
                    await _hubContext.Clients.Group($"Showtime_{showtimeId}").SendAsync("SeatReleased", showtimeId, seatId);
                    await _hubContext.Clients.Group($"showtime-{showtimeId}").SendAsync("SeatReleased", showtimeId, seatId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast SeatReleased event to showtime group {ShowtimeId}", showtimeId);
            }
        }

        /// <summary>
        /// Broadcasts booking confirmation events to the specific showtime SignalR groups.
        /// </summary>
        public async Task BroadcastBookingConfirmedAsync(int showtimeId, List<int> seatIds)
        {
            try
            {
                await _hubContext.Clients.Group($"Showtime_{showtimeId}").SendAsync("BookingConfirmed", showtimeId, seatIds);
                await _hubContext.Clients.Group($"showtime-{showtimeId}").SendAsync("BookingConfirmed", showtimeId, seatIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast BookingConfirmed event to showtime group {ShowtimeId}", showtimeId);
            }
        }

        #endregion

        #region QR Generator

        /// <summary>
        /// Generates a local Base64-encoded PNG QR code from the given text using QRCoder.
        /// </summary>
        private string GenerateBase64QRCode(string text)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate local QR code for text: {Text}", text);
                return string.Empty;
            }
        }

        #endregion

        #region Private Helpers

        private BookingResponseDto MapToResponseDto(BookingDto dto, string? qrCodeUrl)
        {
            return new BookingResponseDto
            {
                BookingId = dto.BookingId,
                BookingCode = dto.BookingCode,
                UserId = dto.UserId,
                UserEmail = dto.UserEmail,
                ShowtimeId = dto.ShowtimeId,
                MovieTitle = dto.MovieTitle,
                StartTime = dto.StartTime,
                HallName = dto.HallName,
                CinemaName = dto.CinemaName,
                TotalAmount = dto.TotalAmount,
                ServiceFee = dto.ServiceFee,
                DiscountAmount = dto.DiscountAmount,
                BookingStatus = dto.BookingStatus,
                CreatedAt = dto.CreatedAt,
                QRCodeUrl = string.IsNullOrEmpty(qrCodeUrl) ? GenerateBase64QRCode(dto.BookingCode) : qrCodeUrl,
                Seats = dto.Seats,
                PaymentUrl = $"/api/payments/retry?bookingId={dto.BookingId}&gateway=VietQR"
            };
        }

        #endregion
    }

    #region Exceptions

    /// <summary>
    /// Represents errors that occur due to business rules violations.
    /// Maps to BadRequest (400) in Global Exception Middleware.
    /// </summary>
    public class BusinessException : InvalidOperationException
    {
        public BusinessException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents errors that occur when a resource is not found.
    /// Maps to NotFound (404) in Global Exception Middleware.
    /// </summary>
    public class NotFoundException : KeyNotFoundException
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents errors that occur during data validation.
    /// Maps to BadRequest (400) in Global Exception Middleware.
    /// </summary>
    public class ValidationException : ArgumentException
    {
        public ValidationException(string message) : base(message) { }
    }

    #endregion

    #region Enums

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Expired
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Cancelled
    }

    public enum PaymentMethod
    {
        VietQR,
        Cash
    }

    #endregion
}
