using System;

namespace CinemaBooking.API.Models.Promotions
{
    public class PromotionCondition
    {
        public int PromotionConditionId { get; set; }

        public int PromotionId { get; set; }
        public Promotion Promotion { get; set; } = null!;

        public PromotionApplyType ApplyType { get; set; }

        public int? MovieId { get; set; }

        public int? ShowtimeId { get; set; }

        public int? MemberLevelId { get; set; } // Points to MemberTierId

        public TimeSpan? StartHour { get; set; }

        public TimeSpan? EndHour { get; set; }

        public string? DaysOfWeek { get; set; } // Comma-separated list of days (e.g. "Monday,Tuesday")
    }
}
