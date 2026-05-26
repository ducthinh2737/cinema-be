namespace CinemaBooking.API.Models.Base
{
    public abstract class BaseEntity<TId>
    {
        public TId Id { get; set; } = default!;
    }
}
