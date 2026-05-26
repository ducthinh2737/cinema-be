namespace CinemaBooking.API.Models.Movies
{
    public class Director
    {
        public int DirectorId { get; set; }

        public string FullName { get; set; } = null!;

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
