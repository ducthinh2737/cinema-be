using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Promotions
{
    public class PromotionConditionDto
    {
        public int PromotionConditionId { get; set; }
        public string ApplyType { get; set; } = null!; // All, Movie, Showtime, GoldenHour, MemberLevel
        public int? MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public int? ShowtimeId { get; set; }
        public int? MemberLevelId { get; set; }
        public string? MemberLevelName { get; set; }
        public TimeSpan? StartHour { get; set; }
        public TimeSpan? EndHour { get; set; }
        public string? DaysOfWeek { get; set; }
    }

    public class PromotionConditionCreateDto
    {
        public string ApplyType { get; set; } = null!;
        public int? MovieId { get; set; }
        public int? ShowtimeId { get; set; }
        public int? MemberLevelId { get; set; }
        public TimeSpan? StartHour { get; set; }
        public TimeSpan? EndHour { get; set; }
        public string? DaysOfWeek { get; set; }
    }

    public class PromotionDto
    {
        public int PromotionId { get; set; }
        public string PromoCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinimumOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public bool IsAutoApply { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PromotionConditionDto> PromotionConditions { get; set; } = new();
    }

    public class PromotionCreateDto
    {
        public string PromoCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinimumOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
        public bool IsAutoApply { get; set; }
        public bool IsActive { get; set; } = true;
        public List<PromotionConditionCreateDto> PromotionConditions { get; set; } = new();
    }

    public class PromotionUpdateDto
    {
        public string PromoCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinimumOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
        public bool IsAutoApply { get; set; }
        public bool IsActive { get; set; }
        public List<PromotionConditionCreateDto> PromotionConditions { get; set; } = new();
    }

    public class PromotionQueryParameters
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? DiscountType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PromotionValidateDto
    {
        public string PromoCode { get; set; } = null!;
        public int? UserId { get; set; }
        public decimal OrderAmount { get; set; }
        public int? MovieId { get; set; }
        public int? ShowtimeId { get; set; }
        public int? MemberLevelId { get; set; }
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
