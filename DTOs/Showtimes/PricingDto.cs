using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Showtimes
{
    public class PricingRuleCreateUpdateDto
    {
        public string SeatType { get; set; } = null!; // "STANDARD" | "VIP" | "COUPLE" | "SWEETBOX" | "ALL"
        public string HallType { get; set; } = null!; // "STANDARD" | "VIP" | "IMAX" | "4DX" | "ALL"
        public string DayType { get; set; } = null!; // "WEEKDAY" | "WEEKEND" | "HOLIDAY" | "ALL"
        public string TimeSlot { get; set; } = null!; // "MORNING" | "AFTERNOON" | "EVENING" | "NIGHT" | "ALL"
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = "ACTIVE"; // "ACTIVE" | "INACTIVE"
        public int Priority { get; set; }
    }

    public class PricingComputeRequestDto
    {
        public string SeatType { get; set; } = null!;
        public int HallId { get; set; }
        public int MovieId { get; set; }
        public int ShowtimeId { get; set; }
        public DateTime? StartTime { get; set; }
    }

    public class PricingComputeResponseDto
    {
        public decimal FinalPrice { get; set; }
        public bool IsSeatTypeAvailable { get; set; }
        public PricingRuleDto? MatchedRule { get; set; }
        public List<string> Breakdown { get; set; } = new List<string>();

        // Structured Log Details
        public int? MatchedRuleId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal SeatSurcharge { get; set; }
        public decimal HallSurcharge { get; set; }
        public decimal AppliedMultiplier { get; set; }
        public string ContextSeatType { get; set; } = string.Empty;
        public string ContextHallType { get; set; } = string.Empty;
        public string ContextDayType { get; set; } = string.Empty;
        public string ContextTimeSlot { get; set; } = string.Empty;
    }

    public class PricingRuleDto
    {
        public int Id { get; set; }
        public string SeatType { get; set; } = null!;
        public string HallType { get; set; } = null!;
        public string DayType { get; set; } = null!;
        public string TimeSlot { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = null!;
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
