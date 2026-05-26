using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaBooking.API.DTOs.Notifications;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] NotificationQueryParameters queryParams)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetPagedNotificationsAsync(userId, queryParams);
            return Ok(result);
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Ok(result);
        }

        [HttpPut("read/{id:int}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Notification with ID {id} not found." });
            }
            return Ok(new { Message = "Notification marked as read successfully." });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { Message = "All notifications marked as read." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var success = await _notificationService.DeleteNotificationAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Notification with ID {id} not found or already deleted." });
            }
            return Ok(new { Message = "Notification has been successfully soft deleted." });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID is missing or invalid in JWT token.");
            }
            return userId;
        }
    }
}
