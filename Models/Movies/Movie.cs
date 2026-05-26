using System;
using System.Collections.Generic;
using CinemaBooking.API.Models.Base;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Models.Movies
{
    public class Movie : SoftDeleteEntity<int>
    {
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public string Language { get; set; } = null!;
        public string? PosterUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime EndDate { get; set; }
        public double Rating { get; set; }

        public int GenreId { get; set; }
        public int AgeRatingId { get; set; }
        public int? DirectorId { get; set; }

        public Genre Genre { get; set; } = null!;
        public Director? Director { get; set; }
        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
