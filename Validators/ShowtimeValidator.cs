using System;
using FluentValidation;
using CinemaBooking.API.DTOs.Showtimes;

namespace CinemaBooking.API.Validators
{
    public class ShowtimeCreateValidator : AbstractValidator<ShowtimeCreateDto>
    {
        public ShowtimeCreateValidator()
        {
            RuleFor(x => x.MovieId)
                .GreaterThan(0).WithMessage("Valid Movie is required.");

            RuleFor(x => x.HallId)
                .GreaterThan(0).WithMessage("Valid Hall is required.");

            RuleFor(x => x.PriceId)
                .GreaterThan(0).WithMessage("Valid Price is required.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start Time is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Start Time must be in the future.");
        }
    }

    public class ShowtimeUpdateValidator : AbstractValidator<ShowtimeUpdateDto>
    {
        public ShowtimeUpdateValidator()
        {
            RuleFor(x => x.MovieId)
                .GreaterThan(0).WithMessage("Valid Movie is required.");

            RuleFor(x => x.HallId)
                .GreaterThan(0).WithMessage("Valid Hall is required.");

            RuleFor(x => x.PriceId)
                .GreaterThan(0).WithMessage("Valid Price is required.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start Time is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Start Time must be in the future.");
        }
    }
}
