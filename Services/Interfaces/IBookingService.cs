using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Bookings;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto?> GetBookingByIdAsync(int id);
        Task<IEnumerable<BookingDto>> GetUserBookingsAsync(int userId);
        Task<BookingDto> CreateBookingAsync(int userId, BookingCreateDto createDto);
        Task<BookingDto> ConfirmBookingAsync(BookingConfirmDto confirmDto);
        Task<bool> CancelBookingAsync(int bookingId);
        Task ExpirePendingBookingsAsync();
    }
}
