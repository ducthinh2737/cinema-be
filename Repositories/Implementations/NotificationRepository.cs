using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Notifications;
using CinemaBooking.API.DTOs.Notifications;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly CinemaDbContext _context;

        public NotificationRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedNotificationsAsync(int userId, NotificationQueryParameters queryParams)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId).AsQueryable();

            // Lọc theo Type
            if (!string.IsNullOrEmpty(queryParams.Type))
            {
                query = query.Where(n => n.Type == queryParams.Type);
            }

            // Lọc theo IsRead
            if (queryParams.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == queryParams.IsRead.Value);
            }

            // Sắp xếp
            if (queryParams.SortBy?.Equals("Title", StringComparison.OrdinalIgnoreCase) == true)
            {
                query = queryParams.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderBy(n => n.Title)
                    : query.OrderByDescending(n => n.Title);
            }
            else
            {
                query = queryParams.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderBy(n => n.CreatedAt)
                    : query.OrderByDescending(n => n.CreatedAt);
            }

            int totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            return await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id);
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            await _context.Notifications.AddAsync(notification);
        }

        public Task UpdateNotificationAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            return Task.CompletedTask;
        }

        public Task DeleteNotificationAsync(Notification notification)
        {
            notification.IsDeleted = true;
            notification.DeletedAt = DateTime.UtcNow;
            _context.Notifications.Update(notification);
            return Task.CompletedTask;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
