using System;

namespace CinemaBooking.API.DTOs.Promotions
{
    public class PromotionDto
    {
        public int PromotionId { get; set; }
        public string PromoCode { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public bool IsActive { get; set; }
    }

    public class PromotionCreateDto
    {
        public string PromoCode { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
    }

    public class PromotionUpdateDto
    {
        public string PromoCode { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
        public bool IsActive { get; set; }
    }

    public class PromotionQueryParameters
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PromotionValidateDto
    {
        public string PromoCode { get; set; } = null!;
        public int UserId { get; set; }
        public decimal OrderAmount { get; set; }
    }

    public class PromotionValidateResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class PromotionApplyDto
    {
        public string PromoCode { get; set; } = null!;
        public int BookingId { get; set; }
    }

    public class PromotionApplyResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public decimal NewTotalAmount { get; set; }
    }
}
