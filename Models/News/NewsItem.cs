using System;
using CinemaBooking.API.Models.Base;

namespace CinemaBooking.API.Models.News
{
    public class NewsItem : SoftDeleteEntity<int>
    {
        public string Title { get; set; } = null!;
        public string? ShortDesc { get; set; }
        public string? Content { get; set; }
        public string? Image { get; set; }
        public string Category { get; set; } = "news"; // news, events
        public string CategoryLabel { get; set; } = "Tin điện ảnh"; // Tin điện ảnh, Sự kiện
        public string Slug { get; set; } = null!;
        public bool Trending { get; set; } = false;
        public string? Tag { get; set; } // for events e.g. "FESTIVAL"
        public string? EventDate { get; set; } // for events e.g. "05/06/2026 - 08/06/2026"
        public bool IsActive { get; set; } = true;
    }
}
