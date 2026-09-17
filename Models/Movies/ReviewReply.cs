using System;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Models.Movies
{
    public class ReviewReply
    {
        public int ReviewReplyId { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public int? ParentReplyId { get; set; }

        public Review Review { get; set; } = null!;
        public User User { get; set; } = null!;
        public ReviewReply? ParentReply { get; set; }
    }
}
