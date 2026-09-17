using System;
using System.Collections.Generic;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Models.Cinemas
{
    public class Hall
    {
        public int HallId { get; set; }
        public int CinemaId { get; set; }
        public string HallName { get; set; } = null!;
        public int HallTypeId { get; set; }
        public int Capacity { get; set; } = 50;
        public string? Description { get; set; }

        // Audit & Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        public Cinema Cinema { get; set; } = null!;
        public HallType HallType { get; set; } = null!;
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}
