using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Showtimes
{
    public class ShowtimeBatchRequestDto
    {
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public int PriceId { get; set; }
        public string Mode { get; set; } = "Manual"; // "Auto" or "Manual"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string IntervalType { get; set; } = "Daily"; // "Daily", "Hourly", "Custom"
        public List<string>? CustomTimes { get; set; } // ["09:00", "13:00", "18:00"]
        public int MaxShowsPerDay { get; set; } = 5;
        public string? IdempotencyKey { get; set; }
    }

    public class ShowtimeBatchResponseDto
    {
        public bool Success { get; set; }
        public List<ShowtimeDto> CreatedShowtimes { get; set; } = new();
        public List<SkippedSlotDto> SkippedSlots { get; set; } = new();
        public List<ConflictDetailDto> Conflicts { get; set; } = new();
        public PerformanceMetricsDto PerformanceMetrics { get; set; } = new();
    }

    public class SkippedSlotDto
    {
        public DateTime StartTime { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class ConflictDetailDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = null!;
        public string? ConflictingShowtimeInfo { get; set; }
    }

    public class PerformanceMetricsDto
    {
        public int DbQueries { get; set; }
        public double ExecutionTimeMs { get; set; }
    }
}
