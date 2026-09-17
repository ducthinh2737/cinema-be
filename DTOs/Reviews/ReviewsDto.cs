using System;

namespace CinemaBooking.API.DTOs.Reviews
{
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerifiedViewer { get; set; }
        public bool IsApproved { get; set; }
        public string Status { get; set; } = "Approved";
        public System.Collections.Generic.List<ReviewReplyDto> Replies { get; set; } = new();
    }

    public class ReviewReplyDto
    {
        public int ReviewReplyId { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? ParentReplyId { get; set; }
        public string? ParentReplyUserName { get; set; }
    }

    public class ReviewReplyCreateDto
    {
        public string Content { get; set; } = null!;
        public int? ParentReplyId { get; set; }
    }

    public class UpdateReviewStatusDto
    {
        public string Status { get; set; } = null!;
    }

    public class MovieRatingSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }

    public class ReviewCreateDto
    {
        public int MovieId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewUpdateDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? MovieId { get; set; }
        public int? Rating { get; set; }
        public string? Status { get; set; }
        public bool? VerifiedOnly { get; set; }
        public string? SearchKeyword { get; set; }
    }

    public class ReviewAnalyticsDto
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int VerifiedReviews { get; set; }
        public int PendingReviews { get; set; }
        public List<RatingDistributionItem> RatingDistribution { get; set; } = new();
        public List<ReviewsPerDayItem> ReviewsPerDay { get; set; } = new();
        public List<TopRatedMovieItem> TopRatedMovies { get; set; } = new();
    }

    public class RatingDistributionItem
    {
        public int Stars { get; set; }
        public int Count { get; set; }
    }

    public class ReviewsPerDayItem
    {
        public string Date { get; set; } = null!;
        public int Count { get; set; }
    }

    public class TopRatedMovieItem
    {
        public string MovieTitle { get; set; } = null!;
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
    }
}
