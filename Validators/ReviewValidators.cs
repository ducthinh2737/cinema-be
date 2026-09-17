using FluentValidation;
using CinemaBooking.API.DTOs.Reviews;

namespace CinemaBooking.API.Validators
{
    public class ReviewCreateDtoValidator : AbstractValidator<ReviewCreateDto>
    {
        public ReviewCreateDtoValidator()
        {
            RuleFor(x => x.MovieId).GreaterThan(0).WithMessage("Movie ID must be greater than 0.");
            RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Review comment is required.")
                .Length(10, 1000).WithMessage("Review comment must be between 10 and 1000 characters.");
        }
    }

    public class ReviewUpdateDtoValidator : AbstractValidator<ReviewUpdateDto>
    {
        public ReviewUpdateDtoValidator()
        {
            RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Review comment is required.")
                .Length(10, 1000).WithMessage("Review comment must be between 10 and 1000 characters.");
        }
    }
}
