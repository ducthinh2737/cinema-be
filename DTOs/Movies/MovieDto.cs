using System;
using System.Collections.Generic;

namespace CinemaBooking.API.DTOs.Movies
{
    public class MovieDto
    {
        public int Id { get; set; }
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
        public string GenreName { get; set; } = null!;
        public int AgeRatingId { get; set; }
        public int? DirectorId { get; set; }
        public string? DirectorName { get; set; }
    }

    public class MovieDetailDto : MovieDto
    {
        public List<MovieActorDto> Actors { get; set; } = new();
    }

    public class MovieActorDto
    {
        public int ActorId { get; set; }
        public string ActorName { get; set; } = null!;
    }

    public class MovieCreateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public string Language { get; set; } = null!;
        public string? TrailerUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime EndDate { get; set; }
        public int GenreId { get; set; }
        public int AgeRatingId { get; set; }
        public int? DirectorId { get; set; }
        public List<int> ActorIds { get; set; } = new();
    }

    public class MovieUpdateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public string Language { get; set; } = null!;
        public string? TrailerUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime EndDate { get; set; }
        public int GenreId { get; set; }
        public int AgeRatingId { get; set; }
        public int? DirectorId { get; set; }
        public List<int> ActorIds { get; set; } = new();
    }

    public class MovieQueryParameters
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        public int? GenreId { get; set; }
        
        // Filter options: "showing", "upcoming", or null (all)
        public string? Status { get; set; } 
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
