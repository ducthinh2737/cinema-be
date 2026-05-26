namespace CinemaBooking.API.Models.Movies
{
    public class Actor
    {
        public int ActorId { get; set; }

        public string FullName { get; set; } = null!;

        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }
}
