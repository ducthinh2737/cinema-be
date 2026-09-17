using System;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Domain.Exceptions;

namespace CinemaBooking.API.Domain.Policies
{
    public class BookingPolicy
    {
        public static void ValidateBookingAllowed(Showtime showtime, DateTime currentUtc)
        {
            if (showtime.StartTime <= currentUtc)
            {
                throw new BusinessException("Không thể đặt vé cho suất chiếu đã hoặc đang diễn ra.");
            }

            // Exclude booking too close to start time (e.g. 10 minutes before)
            if (showtime.StartTime.AddMinutes(-10) <= currentUtc)
            {
                throw new BusinessException("Cổng đặt vé đã đóng (đóng trước giờ chiếu 10 phút).");
            }
        }
    }
}
