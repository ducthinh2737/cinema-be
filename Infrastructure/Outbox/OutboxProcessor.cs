using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.Application.Common.Interfaces;

namespace CinemaBooking.API.Infrastructure.Outbox
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessor> _logger;
        private const int BatchSize = 50;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor background worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxEventsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing outbox events.");
                }

                await Task.Delay(Interval, stoppingToken);
            }

            _logger.LogInformation("Outbox Processor background worker stopped.");
        }

        private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

            // Query unprocessed events
            var events = await dbContext.OutboxEvents
                .Where(e => e.ProcessedOn == null)
                .OrderBy(e => e.OccurredOn)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (!events.Any()) return;

            _logger.LogInformation("Found {Count} unprocessed outbox events.", events.Count);

            foreach (var outboxEvent in events)
            {
                try
                {
                    // Deserialize payload to dynamic/JSON object to avoid typing bottlenecks
                    object? payloadObj = null;
                    if (!string.IsNullOrEmpty(outboxEvent.Payload))
                    {
                        payloadObj = JsonSerializer.Deserialize<JsonElement>(outboxEvent.Payload);
                    }

                    // Dispatch event
                    await eventBus.PublishAsync(outboxEvent.EventName, payloadObj, cancellationToken);

                    outboxEvent.ProcessedOn = DateTime.UtcNow;
                    outboxEvent.Error = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox event {EventId} ({EventName}).", outboxEvent.Id, outboxEvent.EventName);
                    outboxEvent.Error = ex.ToString();
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
