using System;

namespace CinemaBooking.API.Models.Movies
{
    public class MovieFormat
    {
        public int MovieFormatId { get; set; }
        public string FormatName { get; set; } = null!;
        public string? Description { get; set; }

        // Audit & Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
