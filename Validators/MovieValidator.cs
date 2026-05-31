using FluentValidation;
using CinemaBooking.API.DTOs.Movies;

namespace CinemaBooking.API.Validators
{
    public class MovieCreateValidator : AbstractValidator<MovieCreateDto>
    {
        public MovieCreateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(250).WithMessage("Title must not exceed 250 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

            RuleFor(x => x.Duration)
                .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");

            RuleFor(x => x.Language)
                .NotEmpty().WithMessage("Language is required.")
                .MaximumLength(50).WithMessage("Language must not exceed 50 characters.");

            RuleFor(x => x.ReleaseDate)
                .NotEmpty().WithMessage("Release Date is required.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End Date is required.")
                .GreaterThanOrEqualTo(x => x.ReleaseDate).WithMessage("End Date must be greater than or equal to Release Date.");

            RuleFor(x => x.GenreId)
                .GreaterThan(0).WithMessage("Valid Genre is required.");

            RuleFor(x => x.AgeRatingId)
                .GreaterThan(0).WithMessage("Valid Age Rating is required.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(status => new[] { "NowShowing", "ComingSoon", "Ended", "Hidden" }.Contains(status))
                .WithMessage("Status must be one of: NowShowing, ComingSoon, Ended, Hidden.");
        }
    }

    public class MovieUpdateValidator : AbstractValidator<MovieUpdateDto>
    {
        public MovieUpdateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(250).WithMessage("Title must not exceed 250 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

            RuleFor(x => x.Duration)
                .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");

            RuleFor(x => x.Language)
                .NotEmpty().WithMessage("Language is required.")
                .MaximumLength(50).WithMessage("Language must not exceed 50 characters.");

            RuleFor(x => x.ReleaseDate)
                .NotEmpty().WithMessage("Release Date is required.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End Date is required.")
                .GreaterThanOrEqualTo(x => x.ReleaseDate).WithMessage("End Date must be greater than or equal to Release Date.");

            RuleFor(x => x.GenreId)
                .GreaterThan(0).WithMessage("Valid Genre is required.");

            RuleFor(x => x.AgeRatingId)
                .GreaterThan(0).WithMessage("Valid Age Rating is required.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(status => new[] { "NowShowing", "ComingSoon", "Ended", "Hidden" }.Contains(status))
                .WithMessage("Status must be one of: NowShowing, ComingSoon, Ended, Hidden.");
        }
    }
}
