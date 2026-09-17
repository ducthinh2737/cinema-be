using System.Collections.Generic;
using CinemaBooking.API.Models.Base;

namespace CinemaBooking.API.Models.Combos
{
    public class Product : SoftDeleteEntity<int>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();
    }
}
