using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Models.Movies
{
    public class Genre
    {
        public int GenreId { get; set; }

        public string GenreName { get; set; } = null!;
        public string? Description { get; set; }

        // Audit & Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}

