using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.DTOs.Notifications;
using CinemaBooking.API.DTOs.Common;

namespace CinemaBooking.API.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string type);
        Task<PagedResultDto<NotificationDto>> GetPagedNotificationsAsync(int userId, NotificationQueryParameters queryParams);
        Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(int userId);
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int id);
    }
}
