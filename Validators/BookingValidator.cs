using FluentValidation;

namespace CinemaBooking.API.Validators
{
    public class BookingValidator : AbstractValidator<CinemaBooking.API.DTOs.Bookings.BookingDto>
    {
        public BookingValidator()
        {
        }
    }
}
