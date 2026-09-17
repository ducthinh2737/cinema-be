using System;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Models.Movies
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int LikesCount { get; set; } = 0;
        public int DislikesCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerifiedViewer { get; set; } = false;
        public bool IsApproved { get; set; } = true;
        public string Status { get; set; } = "Approved";

        public User User { get; set; } = null!;
        public Movie Movie { get; set; } = null!;
        public List<ReviewReply> Replies { get; set; } = new();
    }
}
