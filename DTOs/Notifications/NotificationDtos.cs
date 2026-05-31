using System;

namespace CinemaBooking.API.DTOs.Notifications
{
    public enum NotificationType
    {
        BookingConfirmed,
        PaymentSuccess,
        PaymentFailed,
        ShowtimeReminder,
        Promotion,
        System,
        Refund,
        BookingCancelled
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Priority { get; set; } = "Normal";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationSummaryDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationDetailDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Priority { get; set; } = "Normal";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationRealtimeDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Priority { get; set; } = "Normal";
        public DateTime CreatedAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationCountDto
    {
        public int UserId { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationQueryParameters
    {
        public string? Type { get; set; }
        public bool? IsRead { get; set; }
        public string? SortBy { get; set; }
        public string SortOrder { get; set; } = "desc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
