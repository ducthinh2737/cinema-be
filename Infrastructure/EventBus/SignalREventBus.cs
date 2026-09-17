using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CinemaBooking.API.Application.Common.Interfaces;
using CinemaBooking.API.SignalR;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.API.Infrastructure.EventBus
{
    public class SignalREventBus : IEventBus
    {
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly ILogger<SignalREventBus> _logger;

        public SignalREventBus(IHubContext<SeatHub> hubContext, ILogger<SignalREventBus> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishAsync<T>(string eventName, T payload, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Broadcasting event '{EventName}' via SignalR Hub.", eventName);
            
            // Safe retry loop for broadcasting SignalR event
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    // Hub method name could be like "ShowtimeCreated", payload is the DTO or ID
                    await _hubContext.Clients.All.SendAsync(eventName, payload, cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < 2)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR event '{EventName}' on attempt {Attempt}. Retrying...", eventName, attempt + 1);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR event '{EventName}' after all attempts.", eventName);
                }
            }
        }
    }
}
