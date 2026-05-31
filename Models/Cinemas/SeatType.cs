using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Models.Cinemas
{
    public class SeatType
    {
        public int SeatTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public decimal PriceMultiplier { get; set; }
        public string? Description { get; set; }

        // Audit & Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
