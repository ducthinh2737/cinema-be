using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Models.Cinemas
{
    public class Cinema
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
        public string? ImageUrl { get; set; }

        // Audit & Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        public City City { get; set; } = null!;
        public ICollection<Hall> Halls { get; set; } = new List<Hall>();
    }
}
