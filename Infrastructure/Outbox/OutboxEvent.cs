using System;

namespace CinemaBooking.API.Infrastructure.Outbox
{
    public class OutboxEvent
    {
        public Guid Id { get; set; }
        public string EventName { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTime OccurredOn { get; set; }
        public DateTime? ProcessedOn { get; set; }
        public string? Error { get; set; }
    }
}
