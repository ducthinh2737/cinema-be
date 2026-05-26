using System;

namespace CinemaBooking.API.Models.Promotions
{
    public class Promotion
    {
        public int PromotionId { get; set; }
        public string PromoCode { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; } = null!; // Percentage, FixedAmount
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public bool IsActive { get; set; } = true;

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
