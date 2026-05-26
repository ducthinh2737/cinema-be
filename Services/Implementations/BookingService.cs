using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
    public class BookingService : IBookingService
    {
        private readonly CinemaDbContext _context;
        private readonly IBookingRepository _bookingRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly ISeatLockService _seatLockService;

        public BookingService(
            CinemaDbContext context,
            IBookingRepository bookingRepository,
            IMapper mapper,
            IHubContext<SeatHub> hubContext,
            ISeatLockService seatLockService)
        {
            _context = context;
            _bookingRepository = bookingRepository;
            _mapper = mapper;
            _hubContext = hubContext;
            _seatLockService = seatLockService;
        }

        public async Task<BookingDto?> GetBookingByIdAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdWithDetailsAsync(id);
            if (booking == null) return null;

            return _mapper.Map<BookingDto>(booking);
        }

        public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(int userId)
        {
            var bookings = await _bookingRepository.GetByUserIdAsync(userId);
            return _mapper.Map<List<BookingDto>>(bookings);
        }

        public async Task<BookingDto> CreateBookingAsync(int userId, BookingCreateDto createDto)
        {
            if (createDto.SeatIds == null || !createDto.SeatIds.Any())
            {
                throw new ArgumentException("Must select at least one seat to book.");
            }

            // 1. Lock seats in distributed cache
            var cacheLocked = await _seatLockService.LockMultipleSeatsAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString());
            if (!cacheLocked)
            {
                throw new InvalidOperationException("One or more selected seats are currently locked by another user.");
            }

            // 2. Chống double booking ghế bằng SQL Transaction mức cô lập Serializable
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Fetch Showtime
                var showtime = await _context.Showtimes
                    .Include(s => s.Price)
                    .Include(s => s.Hall)
                    .FirstOrDefaultAsync(s => s.ShowtimeId == createDto.ShowtimeId);

                if (showtime == null)
                {
                    throw new ArgumentException($"Showtime with ID {createDto.ShowtimeId} not found.");
                }

                // Check active database locks (Not cancelled, and confirmed or created within last 10 minutes)
                var holdThreshold = DateTime.UtcNow.AddMinutes(-10);
                var activeBookingSeatIds = await _context.BookingSeats
                    .Include(bs => bs.Booking)
                    .Where(bs => bs.Booking.ShowtimeId == createDto.ShowtimeId &&
                                 bs.Booking.BookingStatus != "Cancelled" &&
                                 (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.CreatedAt > holdThreshold))
                    .Select(bs => bs.SeatId)
                    .ToListAsync();

                var alreadyBooked = createDto.SeatIds.Intersect(activeBookingSeatIds).ToList();
                if (alreadyBooked.Any())
                {
                    // Rollback cache lock
                    await _seatLockService.UnlockMultipleSeatsAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString());
                    throw new InvalidOperationException("One or more selected seats are already locked or booked.");
                }

                // Load seats details to calculate price with SeatType.PriceMultiplier
                var seats = await _context.Seats
                    .Include(s => s.SeatType)
                    .Where(s => createDto.SeatIds.Contains(s.SeatId))
                    .ToListAsync();

                if (seats.Count != createDto.SeatIds.Count)
                {
                    await _seatLockService.UnlockMultipleSeatsAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString());
                    throw new ArgumentException("One or more selected seat IDs are invalid.");
                }

                // Tính giá vé
                decimal subtotal = 0;
                var bookingSeatsList = new List<BookingSeat>();

                foreach (var seat in seats)
                {
                    decimal seatPrice = showtime.Price.Value * seat.SeatType.PriceMultiplier;
                    subtotal += seatPrice;

                    bookingSeatsList.Add(new BookingSeat
                    {
                        SeatId = seat.SeatId,
                        UnitPrice = seatPrice
                    });
                }

                // Phí dịch vụ: 5,000 VND / ghế
                decimal serviceFee = seats.Count * 5000m;

                // Khuyến mãi (Voucher)
                decimal discount = 0;
                Promotion? promotion = null;

                if (!string.IsNullOrEmpty(createDto.PromoCode))
                {
                    promotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.PromoCode == createDto.PromoCode);

                    if (promotion != null)
                    {
                        if (promotion.DiscountValue <= 1.0m)
                        {
                            discount = subtotal * promotion.DiscountValue;
                        }
                        else
                        {
                            discount = promotion.DiscountValue;
                        }
                    }
                }

                decimal totalAmount = Math.Max(0, subtotal + serviceFee - discount);

                // Unique Booking Code
                var bookingCode = $"BK-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    UserId = userId,
                    ShowtimeId = createDto.ShowtimeId,
                    TotalAmount = totalAmount,
                    ServiceFee = serviceFee,
                    DiscountAmount = discount,
                    BookingStatus = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    BookingSeats = bookingSeatsList
                };

                await _context.Bookings.AddAsync(booking);
                await _context.SaveChangesAsync();

                // If promo applied, link it
                if (promotion != null)
                {
                    var bookingPromo = new BookingPromotion
                    {
                        BookingId = booking.BookingId,
                        PromotionId = promotion.PromotionId
                    };
                    await _context.BookingPromotions.AddAsync(bookingPromo);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Broadcast SeatSelected events to all SignalR clients
                foreach (var seatId in createDto.SeatIds)
                {
                    await _hubContext.Clients.All.SendAsync("SeatSelected", createDto.ShowtimeId, seatId, userId.ToString());
                }

                var savedBooking = await _bookingRepository.GetByIdWithDetailsAsync(booking.BookingId);
                return _mapper.Map<BookingDto>(savedBooking ?? booking);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                await _seatLockService.UnlockMultipleSeatsAsync(createDto.ShowtimeId, createDto.SeatIds, userId.ToString());
                throw;
            }
        }

        public async Task<BookingDto> ConfirmBookingAsync(BookingConfirmDto confirmDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                    .FirstOrDefaultAsync(b => b.BookingId == confirmDto.BookingId);

                if (booking == null)
                {
                    throw new ArgumentException($"Booking with ID {confirmDto.BookingId} not found.");
                }

                if (booking.BookingStatus == "Confirmed")
                {
                    return _mapper.Map<BookingDto>(booking);
                }

                if (booking.BookingStatus == "Cancelled")
                {
                    throw new InvalidOperationException("Cannot confirm a cancelled booking.");
                }

                // Check Hold Expiration (10 minutes hold)
                if (booking.CreatedAt < DateTime.UtcNow.AddMinutes(-10))
                {
                    booking.BookingStatus = "Cancelled";
                    _context.Bookings.Update(booking);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Realtime seat unlock notification via SignalR
                    var expiredSeatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                    await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, expiredSeatIds, booking.UserId.ToString());
                    foreach (var seatId in expiredSeatIds)
                    {
                        await _hubContext.Clients.All.SendAsync("SeatReleased", booking.ShowtimeId, seatId);
                    }

                    throw new InvalidOperationException("Booking hold has expired. Seats have been unlocked.");
                }

                // Confirm booking
                booking.BookingStatus = "Confirmed";
                
                // Real working QR code API
                booking.QRCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={booking.BookingCode}";

                // Create success payment record
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = booking.TotalAmount,
                    PaymentMethod = confirmDto.PaymentMethod,
                    PaymentStatus = "Success",
                    PaymentDate = DateTime.UtcNow
                };
                await _context.Payments.AddAsync(payment);

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Unlock from cache and broadcast BookingConfirmed
                var confirmedSeatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, confirmedSeatIds, booking.UserId.ToString());
                await _hubContext.Clients.All.SendAsync("BookingConfirmed", booking.ShowtimeId, confirmedSeatIds);

                return _mapper.Map<BookingDto>(booking);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null) return false;

                if (booking.BookingStatus == "Cancelled") return true;

                booking.BookingStatus = "Cancelled";
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Realtime seat unlock notification via SignalR
                var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, seatIds, booking.UserId.ToString());
                foreach (var seatId in seatIds)
                {
                    await _hubContext.Clients.All.SendAsync("SeatReleased", booking.ShowtimeId, seatId);
                }

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ExpirePendingBookingsAsync()
        {
            var threshold = DateTime.UtcNow.AddMinutes(-10);
            var expiredPendingBookings = await _bookingRepository.GetExpiredPendingBookingsAsync(threshold);

            if (!expiredPendingBookings.Any()) return;

            foreach (var booking in expiredPendingBookings)
            {
                booking.BookingStatus = "Cancelled";
                _bookingRepository.Update(booking);
            }

            await _bookingRepository.SaveChangesAsync();

            // Realtime SignalR notification for all unlocked seats
            foreach (var booking in expiredPendingBookings)
            {
                var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                await _seatLockService.UnlockMultipleSeatsAsync(booking.ShowtimeId, seatIds, booking.UserId.ToString());
                foreach (var seatId in seatIds)
                {
                    await _hubContext.Clients.All.SendAsync("SeatReleased", booking.ShowtimeId, seatId);
                }
            }
        }
    }
}
