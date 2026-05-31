using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Models.Notifications;
using CinemaBooking.API.DTOs.Notifications;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Enterprise implementation of realtime notification services.
    /// Manages database lifecycle, MemoryCache layers, and optimizes SignalR broadcasts to multiple devices.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext,
            IMemoryCache cache,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _mapper = mapper;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Sends a notification to a specific user and broadcasts it in real-time.
        /// </summary>
        public async Task<ApiResponse<NotificationDetailDto>> SendNotificationAsync(
            int userId, 
            string title, 
            string message, 
            NotificationType type, 
            NotificationPriority priority = NotificationPriority.Normal)
        {
            _logger.LogInformation("Sending notification to user {UserId}. Type: {Type}, Priority: {Priority}", userId, type, priority);

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type.ToString(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddNotificationAsync(notification);
            var saved = await _notificationRepository.SaveChangesAsync();

            if (!saved)
            {
                _logger.LogError("Failed to save notification for user {UserId}", userId);
                return ApiResponse.Fail<NotificationDetailDto>("Failed to send notification.");
            }

            // Invalidate cache
            InvalidateUnreadCountCache(userId);

            var detailDto = _mapper.Map<NotificationDetailDto>(notification);
            detailDto.Priority = priority.ToString();

            // Fetch updated unread count
            var countResult = await GetUnreadCountAsync(userId);
            var unreadCount = countResult.Data?.UnreadCount ?? 0;

            // Broadcast SignalR
            var realtimeDto = new NotificationRealtimeDto
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = priority.ToString(),
                CreatedAt = notification.CreatedAt,
                UnreadCount = unreadCount
            };

            await BroadcastRealtimeAsync(userId, "ReceiveNotification", realtimeDto);
            await BroadcastRealtimeAsync(userId, "UnreadCountUpdated", new { UnreadCount = unreadCount });

            return ApiResponse.Success(detailDto);
        }

        /// <summary>
        /// Sends bulk notifications to multiple users (e.g. system alerts, promotions) in exactly one DB batch.
        /// </summary>
        public async Task<ApiResponse<bool>> SendBulkNotificationAsync(
            List<int> userIds, 
            string title, 
            string message, 
            NotificationType type, 
            NotificationPriority priority = NotificationPriority.Normal)
        {
            if (userIds == null || !userIds.Any())
            {
                return ApiResponse.Success(true);
            }

            _logger.LogInformation("Sending bulk notification to {Count} users. Type: {Type}", userIds.Count, type);

            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type.ToString(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            foreach (var n in notifications)
            {
                await _notificationRepository.AddNotificationAsync(n);
            }

            var saved = await _notificationRepository.SaveChangesAsync();
            if (!saved)
            {
                _logger.LogError("Failed to save bulk notifications.");
                return ApiResponse.Fail<bool>("Failed to send bulk notifications.");
            }

            // Invalidate caches and broadcast SignalR messages in parallel
            var broadcastTasks = userIds.Select(async userId =>
            {
                InvalidateUnreadCountCache(userId);

                var countResult = await GetUnreadCountAsync(userId);
                var unreadCount = countResult.Data?.UnreadCount ?? 0;

                var correspondingNotification = notifications.First(n => n.UserId == userId);

                var realtimeDto = new NotificationRealtimeDto
                {
                    NotificationId = correspondingNotification.NotificationId,
                    Title = correspondingNotification.Title,
                    Message = correspondingNotification.Message,
                    Type = correspondingNotification.Type,
                    Priority = priority.ToString(),
                    CreatedAt = correspondingNotification.CreatedAt,
                    UnreadCount = unreadCount
                };

                await BroadcastRealtimeAsync(userId, "ReceiveNotification", realtimeDto);
                await BroadcastRealtimeAsync(userId, "UnreadCountUpdated", new { UnreadCount = unreadCount });
            });

            await Task.WhenAll(broadcastTasks);

            return ApiResponse.Success(true);
        }

        /// <summary>
        /// Retrieves a paged list of notifications for a user.
        /// </summary>
        public async Task<ApiResponse<PagedResultDto<NotificationDto>>> GetPagedNotificationsAsync(int userId, NotificationQueryParameters queryParams)
        {
            var (notifications, totalCount) = await _notificationRepository.GetPagedNotificationsAsync(userId, queryParams);
            var dtos = _mapper.Map<List<NotificationDto>>(notifications);

            var pagedResult = new PagedResultDto<NotificationDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse.Success(pagedResult);
        }

        /// <summary>
        /// Retrieves all unread notifications for a user.
        /// </summary>
        public async Task<ApiResponse<IEnumerable<NotificationDto>>> GetUnreadNotificationsAsync(int userId)
        {
            var notifications = await _notificationRepository.GetUnreadNotificationsAsync(userId);
            var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
            return ApiResponse.Success(dtos);
        }

        /// <summary>
        /// Retrieves the current unread count for a user, using cache first.
        /// </summary>
        public async Task<ApiResponse<NotificationCountDto>> GetUnreadCountAsync(int userId)
        {
            var cacheKey = $"UnreadCount_{userId}";
            if (_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                return ApiResponse.Success(new NotificationCountDto { UserId = userId, UnreadCount = cachedCount });
            }

            var unreadNotifications = await _notificationRepository.GetUnreadNotificationsAsync(userId);
            var count = unreadNotifications.Count();

            _cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));

            return ApiResponse.Success(new NotificationCountDto { UserId = userId, UnreadCount = count });
        }

        /// <summary>
        /// Marks a single notification as read, updating cache and broadcasting SignalR status.
        /// </summary>
        public async Task<ApiResponse<bool>> MarkAsReadAsync(int id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null)
            {
                return ApiResponse.Fail<bool>("Notification not found.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateNotificationAsync(notification);
                await _notificationRepository.SaveChangesAsync();

                InvalidateUnreadCountCache(notification.UserId);

                var countResult = await GetUnreadCountAsync(notification.UserId);
                var unreadCount = countResult.Data?.UnreadCount ?? 0;

                await BroadcastRealtimeAsync(notification.UserId, "NotificationRead", new { NotificationId = id });
                await BroadcastRealtimeAsync(notification.UserId, "UnreadCountUpdated", new { UnreadCount = unreadCount });
            }

            return ApiResponse.Success(true);
        }

        /// <summary>
        /// Marks all unread notifications as read for a user.
        /// </summary>
        public async Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
            await _notificationRepository.SaveChangesAsync();

            InvalidateUnreadCountCache(userId);

            await BroadcastRealtimeAsync(userId, "UnreadCountUpdated", new { UnreadCount = 0 });

            return ApiResponse.Success(true);
        }

        /// <summary>
        /// Soft deletes a notification, invalidating count cache.
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteNotificationAsync(int id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null)
            {
                return ApiResponse.Fail<bool>("Notification not found.");
            }

            var userId = notification.UserId;
            var wasUnread = !notification.IsRead;

            await _notificationRepository.DeleteNotificationAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            if (wasUnread)
            {
                InvalidateUnreadCountCache(userId);
            }

            var countResult = await GetUnreadCountAsync(userId);
            var unreadCount = countResult.Data?.UnreadCount ?? 0;

            await BroadcastRealtimeAsync(userId, "NotificationDeleted", new { NotificationId = id });
            await BroadcastRealtimeAsync(userId, "UnreadCountUpdated", new { UnreadCount = unreadCount });

            return ApiResponse.Success(true);
        }

        /// <summary>
        /// Safely broadcasts real-time payloads to SignalR clients.
        /// </summary>
        public async Task BroadcastRealtimeAsync(int userId, string eventName, object data)
        {
            try
            {
                await _hubContext.Clients.Group($"User_{userId}").SendAsync(eventName, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast event {Event} via SignalR to user {UserId}", eventName, userId);
            }
        }

        #region Private Helper Methods

        private void InvalidateUnreadCountCache(int userId)
        {
            var cacheKey = $"UnreadCount_{userId}";
            _cache.Remove(cacheKey);
        }

        #endregion
    }
}
