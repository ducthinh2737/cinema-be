using FluentValidation;

namespace CinemaBooking.API.Validators
{
    public class PaymentValidator : AbstractValidator<CinemaBooking.API.DTOs.Payments.PaymentDto>
    {
        public PaymentValidator()
        {
        }
    }
}
