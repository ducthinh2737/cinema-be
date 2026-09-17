using System;
using FluentValidation;
using CinemaBooking.API.DTOs.Promotions;

namespace CinemaBooking.API.Validators
{
    public class PromotionCreateDtoValidator : AbstractValidator<PromotionCreateDto>
    {
        public PromotionCreateDtoValidator()
        {
            RuleFor(x => x.PromoCode).NotEmpty().WithMessage("Promo code is required.").MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
            RuleFor(x => x.DiscountType).NotEmpty().Must(type => type == "Percentage" || type == "FixedAmount")
                .WithMessage("Discount type must be either 'Percentage' or 'FixedAmount'.");
            RuleFor(x => x.DiscountValue).GreaterThan(0).WithMessage("Discount value must be greater than 0.");
            RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
            RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
            RuleFor(x => x.MaxUsage).GreaterThan(0).WithMessage("Max usage limit must be greater than 0.");
        }
    }

    public class PromotionUpdateDtoValidator : AbstractValidator<PromotionUpdateDto>
    {
        public PromotionUpdateDtoValidator()
        {
            RuleFor(x => x.PromoCode).NotEmpty().WithMessage("Promo code is required.").MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
            RuleFor(x => x.DiscountType).NotEmpty().Must(type => type == "Percentage" || type == "FixedAmount")
                .WithMessage("Discount type must be either 'Percentage' or 'FixedAmount'.");
            RuleFor(x => x.DiscountValue).GreaterThan(0).WithMessage("Discount value must be greater than 0.");
            RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
            RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
            RuleFor(x => x.MaxUsage).GreaterThan(0).WithMessage("Max usage limit must be greater than 0.");
        }
    }

    public class PromotionValidateDtoValidator : AbstractValidator<PromotionValidateDto>
    {
        public PromotionValidateDtoValidator()
        {
            RuleFor(x => x.PromoCode).NotEmpty().WithMessage("Promo code is required.").MaximumLength(50);
            RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue).WithMessage("User ID must be greater than 0.");
            RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0).WithMessage("Order amount must be non-negative.");
        }
    }

    public class PromotionApplyDtoValidator : AbstractValidator<PromotionApplyDto>
    {
        public PromotionApplyDtoValidator()
        {
            RuleFor(x => x.PromoCode).NotEmpty().WithMessage("Promo code is required.").MaximumLength(50);
            RuleFor(x => x.BookingId).GreaterThan(0).WithMessage("Booking ID must be greater than 0.");
        }
    }
}
