using System.Collections.Generic;
using CinemaBooking.API.Models.Base;

namespace CinemaBooking.API.Models.Combos
{
    public class Combo : SoftDeleteEntity<int>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? DiscountBadge { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        public ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();
    }
}
