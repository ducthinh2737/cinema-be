using FluentValidation;
using CinemaBooking.API.DTOs.Cinemas;

namespace CinemaBooking.API.Validators
{
    // Cinema
    public class CinemaCreateDtoValidator : AbstractValidator<CinemaCreateDto>
    {
        public CinemaCreateDtoValidator()
        {
            RuleFor(x => x.CinemaName).NotEmpty().WithMessage("Cinema name is required.").MaximumLength(150);
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.").MaximumLength(250);
            RuleFor(x => x.CityId).GreaterThan(0).WithMessage("City ID must be greater than 0.");
        }
    }

    public class CinemaUpdateDtoValidator : AbstractValidator<CinemaUpdateDto>
    {
        public CinemaUpdateDtoValidator()
        {
            RuleFor(x => x.CinemaName).NotEmpty().WithMessage("Cinema name is required.").MaximumLength(150);
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.").MaximumLength(250);
            RuleFor(x => x.CityId).GreaterThan(0).WithMessage("City ID must be greater than 0.");
        }
    }

    // Hall
    public class HallCreateDtoValidator : AbstractValidator<HallCreateDto>
    {
        public HallCreateDtoValidator()
        {
            RuleFor(x => x.CinemaId).GreaterThan(0).WithMessage("Cinema ID must be greater than 0.");
            RuleFor(x => x.HallName).NotEmpty().WithMessage("Hall name is required.").MaximumLength(100);
            RuleFor(x => x.HallTypeId).GreaterThan(0).WithMessage("Hall type ID must be greater than 0.");
        }
    }

    public class HallUpdateDtoValidator : AbstractValidator<HallUpdateDto>
    {
        public HallUpdateDtoValidator()
        {
            RuleFor(x => x.HallName).NotEmpty().WithMessage("Hall name is required.").MaximumLength(100);
            RuleFor(x => x.HallTypeId).GreaterThan(0).WithMessage("Hall type ID must be greater than 0.");
        }
    }

    // Seat
    public class SeatCreateDtoValidator : AbstractValidator<SeatCreateDto>
    {
        public SeatCreateDtoValidator()
        {
            RuleFor(x => x.HallId).GreaterThan(0).WithMessage("Hall ID must be greater than 0.");
            RuleFor(x => x.SeatCode).NotEmpty().WithMessage("Seat code is required.").MaximumLength(10);
            RuleFor(x => x.SeatTypeId).GreaterThan(0).WithMessage("Seat type ID must be greater than 0.");
        }
    }

    public class SeatUpdateDtoValidator : AbstractValidator<SeatUpdateDto>
    {
        public SeatUpdateDtoValidator()
        {
            RuleFor(x => x.SeatCode).NotEmpty().WithMessage("Seat code is required.").MaximumLength(10);
            RuleFor(x => x.SeatTypeId).GreaterThan(0).WithMessage("Seat type ID must be greater than 0.");
        }
    }

    // HallType
    public class HallTypeCreateDtoValidator : AbstractValidator<HallTypeCreateDto>
    {
        public HallTypeCreateDtoValidator()
        {
            RuleFor(x => x.TypeName).NotEmpty().WithMessage("Type name is required.").MaximumLength(50);
        }
    }

    public class HallTypeUpdateDtoValidator : AbstractValidator<HallTypeUpdateDto>
    {
        public HallTypeUpdateDtoValidator()
        {
            RuleFor(x => x.TypeName).NotEmpty().WithMessage("Type name is required.").MaximumLength(50);
        }
    }

    // SeatType
    public class SeatTypeCreateDtoValidator : AbstractValidator<SeatTypeCreateDto>
    {
        public SeatTypeCreateDtoValidator()
        {
            RuleFor(x => x.TypeName).NotEmpty().WithMessage("Type name is required.").MaximumLength(50);
        }
    }

    public class SeatTypeUpdateDtoValidator : AbstractValidator<SeatTypeUpdateDto>
    {
        public SeatTypeUpdateDtoValidator()
        {
            RuleFor(x => x.TypeName).NotEmpty().WithMessage("Type name is required.").MaximumLength(50);
        }
    }
}
