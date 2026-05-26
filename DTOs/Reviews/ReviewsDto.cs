using System;

namespace CinemaBooking.API.DTOs.Reviews
{
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int LikesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewCreateDto
    {
        public int MovieId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewUpdateDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
