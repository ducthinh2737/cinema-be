using System;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Models.Notifications
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!; // Booking, Payment, Promotion, System
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
