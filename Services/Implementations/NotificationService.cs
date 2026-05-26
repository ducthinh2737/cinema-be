using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using CinemaBooking.API.Models.Notifications;
using CinemaBooking.API.DTOs.Notifications;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddNotificationAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            // Real-time broadcast
            var notificationDto = _mapper.Map<NotificationDto>(notification);
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notificationDto);
        }

        public async Task<PagedResultDto<NotificationDto>> GetPagedNotificationsAsync(int userId, NotificationQueryParameters queryParams)
        {
            var (notifications, totalCount) = await _notificationRepository.GetPagedNotificationsAsync(userId, queryParams);
            var dtos = _mapper.Map<List<NotificationDto>>(notifications);

            return new PagedResultDto<NotificationDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(int userId)
        {
            var notifications = await _notificationRepository.GetUnreadNotificationsAsync(userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null) return false;

            notification.IsRead = true;
            await _notificationRepository.UpdateNotificationAsync(notification);
            return await _notificationRepository.SaveChangesAsync();
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
            return await _notificationRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null) return false;

            await _notificationRepository.DeleteNotificationAsync(notification);
            return await _notificationRepository.SaveChangesAsync();
        }
    }
}
