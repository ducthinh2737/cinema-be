using System;

namespace CinemaBooking.API.DTOs.News
{
    public class NewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? ShortDesc { get; set; }
        public string? Content { get; set; }
        public string? Image { get; set; }
        public string Category { get; set; } = "news";
        public string CategoryLabel { get; set; } = "Tin điện ảnh";
        public string Slug { get; set; } = null!;
        public bool Trending { get; set; }
        public string? Tag { get; set; }
        public string? EventDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Computed properties for frontend compatibility with NewsItem and EventItem
        public string Date => Category == "events" && !string.IsNullOrWhiteSpace(EventDate) 
            ? EventDate 
            : CreatedAt.ToString("dd/MM/yyyy");
        public string? Description => ShortDesc;
    }

    public class NewsCreateDto
    {
        public string Title { get; set; } = null!;
        public string? ShortDesc { get; set; }
        public string? Content { get; set; }
        public string? Image { get; set; }
        public string Category { get; set; } = "news";
        public string CategoryLabel { get; set; } = "Tin điện ảnh";
        public string Slug { get; set; } = null!;
        public bool Trending { get; set; }
        public string? Tag { get; set; }
        public string? EventDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class NewsUpdateDto
    {
        public string Title { get; set; } = null!;
        public string? ShortDesc { get; set; }
        public string? Content { get; set; }
        public string? Image { get; set; }
        public string Category { get; set; } = "news";
        public string CategoryLabel { get; set; } = "Tin điện ảnh";
        public string Slug { get; set; } = null!;
        public bool Trending { get; set; }
        public string? Tag { get; set; }
        public string? EventDate { get; set; }
        public bool IsActive { get; set; }
    }
}
