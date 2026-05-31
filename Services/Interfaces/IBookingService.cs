using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Bookings;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Enterprise Cinema Booking Service Interface.
    /// Handles secure, concurrent, and transaction-safe booking operations.
    /// </summary>
    public interface IBookingService
    {
        Task<ApiResponse<BookingResponseDto>> GetBookingByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<IEnumerable<BookingResponseDto>>> GetUserBookingsAsync(int userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<IEnumerable<BookingResponseDto>>> GetAllBookingsAsync(CancellationToken cancellationToken = default);

        Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(int userId, BookingCreateDto createDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<BookingResponseDto>> ConfirmBookingAsync(BookingConfirmDto confirmDto, CancellationToken cancellationToken = default);
        Task<ApiResponse<BookingResponseDto>> ConfirmPaymentAsync(int bookingId, string paymentMethod, CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> CancelBookingAsync(int bookingId, CancellationToken cancellationToken = default);
        Task ExpirePendingBookingsAsync(CancellationToken cancellationToken = default);
        Task ReleaseExpiredBookingsAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse<bool>> ValidateSeatsAsync(int showtimeId, List<int> seatIds, string userId, CancellationToken cancellationToken = default);
        Task<ApiResponse<decimal>> CalculateTotalAmountAsync(int showtimeId, List<int> seatIds, string? promoCode, CancellationToken cancellationToken = default);
        Task<ApiResponse<string>> GenerateBookingCodeAsync(CancellationToken cancellationToken = default);
    }
}
