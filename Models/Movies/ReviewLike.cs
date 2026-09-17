using System;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Models.Movies
{
    public class ReviewLike
    {
        public int ReviewLikeId { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        public Review Review { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
