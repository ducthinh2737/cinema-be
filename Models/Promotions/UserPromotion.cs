using System;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Models.Promotions
{
    public class UserPromotion
    {
        public int UserPromotionId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PromotionId { get; set; }
        public Promotion Promotion { get; set; } = null!;
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }
    }
}
