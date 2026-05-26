namespace CinemaBooking.API.Models.Logs
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = null!;

        public string TableName { get; set; } = null!;

        public int RecordId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
