using System.Collections.Generic;

namespace CinemaBooking.API.Models.Cinemas
{
    public class City
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = null!;
        public ICollection<Cinema> Cinemas { get; set; } = new List<Cinema>();
    }
}
