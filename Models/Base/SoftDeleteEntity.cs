using System;

namespace CinemaBooking.API.Models.Base
{
    public abstract class SoftDeleteEntity<TId> : BaseAuditEntity<TId>
    {
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}
