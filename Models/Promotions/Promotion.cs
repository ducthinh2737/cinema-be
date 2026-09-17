using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Models.Promotions
{
    public class Promotion
    {
        public int PromotionId { get; set; }
        
        public string PromoCode { get; set; } = null!;
        
        public string Name { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public PromotionType DiscountType { get; set; }
        
        public decimal DiscountValue { get; set; }
        
        public decimal? MaxDiscountAmount { get; set; }
        
        public decimal? MinimumOrderValue { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public int MaxUsage { get; set; } // UsageLimit
        
        public int CurrentUsage { get; set; } // UsedCount
        
        public bool IsAutoApply { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public ICollection<PromotionCondition> PromotionConditions { get; set; } = new List<PromotionCondition>();
    }
}
