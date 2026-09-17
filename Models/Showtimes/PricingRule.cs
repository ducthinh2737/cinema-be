using System;

namespace CinemaBooking.API.Models.Showtimes
{
    public class PricingRule
    {
        public int Id { get; set; }
        public string SeatType { get; set; } = null!; // "STANDARD" | "VIP" | "COUPLE" | "SWEETBOX"
        public string HallType { get; set; } = null!; // "STANDARD" | "VIP" | "IMAX" | "4DX"
        public string DayType { get; set; } = null!; // "WEEKDAY" | "WEEKEND" | "HOLIDAY"
        public string TimeSlot { get; set; } = null!; // "MORNING" | "AFTERNOON" | "EVENING" | "NIGHT"
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = "ACTIVE"; // "ACTIVE" | "INACTIVE"
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
