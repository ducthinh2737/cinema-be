using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Notifications;
using CinemaBooking.API.DTOs.Common;

namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Enterprise Realtime Notification Service for customer messaging, promotions, reminders, and auditing.
    /// </summary>
    public interface INotificationService
    {
        Task<ApiResponse<NotificationDetailDto>> SendNotificationAsync(int userId, string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal);
        Task<ApiResponse<bool>> SendBulkNotificationAsync(List<int> userIds, string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal);
        Task<ApiResponse<PagedResultDto<NotificationDto>>> GetPagedNotificationsAsync(int userId, NotificationQueryParameters queryParams);
        Task<ApiResponse<IEnumerable<NotificationDto>>> GetUnreadNotificationsAsync(int userId);
        Task<ApiResponse<NotificationCountDto>> GetUnreadCountAsync(int userId);
        Task<ApiResponse<bool>> MarkAsReadAsync(int id);
        Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId);
        Task<ApiResponse<bool>> DeleteNotificationAsync(int id);
        Task BroadcastRealtimeAsync(int userId, string eventName, object data);
    }
}
