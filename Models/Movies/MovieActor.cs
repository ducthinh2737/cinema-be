namespace CinemaBooking.API.Models.Movies
{
    public class MovieActor
    {
        public int MovieActorId { get; set; }

        public int MovieId { get; set; }

        public int ActorId { get; set; }

        public Movie Movie { get; set; } = null!;

        public Actor Actor { get; set; } = null!;
    }
}
