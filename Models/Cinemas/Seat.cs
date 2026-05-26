using System;

namespace CinemaBooking.API.Models.Cinemas
{
    public class Seat
    {
        public int SeatId { get; set; }
        public int HallId { get; set; }
        public string SeatCode { get; set; } = null!;
        public int SeatTypeId { get; set; }

        // Audit & Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        public Hall Hall { get; set; } = null!;
        public SeatType SeatType { get; set; } = null!;
    }
}
