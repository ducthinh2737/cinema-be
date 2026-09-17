using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Users
{
    public class LoyaltyDashboardDto
    {
        public string TierName { get; set; } = null!;
        public int LoyaltyPoints { get; set; }
        public int LifetimePoints { get; set; }
        public decimal PointMultiplier { get; set; }
        public string? BenefitsDescription { get; set; }
        public decimal CashEquivalentValue { get; set; }  // Cash value in VNĐ (e.g. Points * 1000)
        
        // Progression to next tier
        public string? NextTierName { get; set; }
        public int? PointsNeededForNextTier { get; set; }
        public double ProgressionPercent { get; set; }
    }

    public class LoyaltyTransactionDto
    {
        public int LoyaltyTransactionId { get; set; }
        public int UserId { get; set; }
        public string? UserFullName { get; set; }
        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public int PointsChanged { get; set; }
        public string TransactionType { get; set; } = null!; // Earn, Redeem, Refund, EventBonus, ManualAdjustment
        public string Status { get; set; } = null!;          // Pending, Completed, Cancelled
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LoyaltyRedeemValidateRequestDto
    {
        public int ShowtimeId { get; set; }
        public List<int> SeatIds { get; set; } = new List<int>();
        public int PointsToRedeem { get; set; }
    }

    public class LoyaltyRedeemValidateResultDto
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OriginalTotalAmount { get; set; }
        public decimal FinalTotalAmount { get; set; }
        public string? Message { get; set; }
    }

    public class LoyaltyAdminStatsDto
    {
        public long TotalPointsIssued { get; set; }
        public long TotalPointsRedeemed { get; set; }
        public long TotalPointsCirculating { get; set; }
        public Dictionary<string, int> MemberCountByTier { get; set; } = new Dictionary<string, int>();
        public List<TopMemberDto> TopLoyalMembers { get; set; } = new List<TopMemberDto>();
    }

    public class TopMemberDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int LoyaltyPoints { get; set; }
        public int LifetimePoints { get; set; }
        public string TierName { get; set; } = null!;
    }

    public class LoyaltyPointsAdjustDto
    {
        public int UserId { get; set; }
        public int Points { get; set; } // Can be positive or negative
        public string Description { get; set; } = null!;
    }
}
