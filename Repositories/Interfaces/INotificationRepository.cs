using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBooking.API.Models.Notifications;
using CinemaBooking.API.DTOs.Notifications;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedNotificationsAsync(int userId, NotificationQueryParameters queryParams);
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification?> GetNotificationByIdAsync(int id);
        Task AddNotificationAsync(Notification notification);
        Task AddRangeAsync(IEnumerable<Notification> notifications);
        Task UpdateNotificationAsync(Notification notification);
        Task DeleteNotificationAsync(Notification notification);
        Task MarkAllAsReadAsync(int userId);
        Task<bool> SaveChangesAsync();
    }
}
