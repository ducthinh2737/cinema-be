using System;

namespace CinemaBooking.API.Models.Base
{
    public abstract class BaseAuditEntity<TId> : BaseEntity<TId>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
