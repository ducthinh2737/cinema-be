using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Reviews;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.Domain.Exceptions;

namespace CinemaBooking.API.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;
        private readonly CinemaDbContext _context;

        public ReviewService(IReviewRepository reviewRepository, IMapper mapper, CinemaDbContext context)
        {
            _reviewRepository = reviewRepository;
            _mapper = mapper;
            _context = context;
        }

        public async Task<CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>> GetPagedReviewsByMovieIdAsync(int movieId, ReviewQueryParameters queryParams)
        {
            var (reviews, totalCount) = await _reviewRepository.GetPagedReviewsByMovieIdAsync(movieId, queryParams);
            var dtos = _mapper.Map<List<ReviewDto>>(reviews);

            return new CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>> GetPagedReviewsByUserIdAsync(int userId, ReviewQueryParameters queryParams)
        {
            var (reviews, totalCount) = await _reviewRepository.GetPagedReviewsByUserIdAsync(userId, queryParams);
            var dtos = _mapper.Map<List<ReviewDto>>(reviews);

            return new CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ReviewDto?> GetReviewByIdAsync(int id)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            return _mapper.Map<ReviewDto>(review);
        }

        public async Task<ReviewDto> CreateReviewAsync(int userId, ReviewCreateDto createDto)
        {
            // 1. Check user exists and is active (not banned)
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new BusinessException("Người dùng không tồn tại trên hệ thống.");
            }
            if (!user.IsActive)
            {
                throw new BusinessException("Tài khoản của bạn đã bị khóa hoặc vô hiệu hóa. Không thể thực hiện đánh giá.");
            }

            // 2. Check user has already reviewed this movie
            var existing = await _reviewRepository.GetReviewByUserAndMovieAsync(userId, createDto.MovieId);
            if (existing != null)
            {
                throw new BusinessException("Bạn đã đánh giá bộ phim này rồi. Mỗi khách hàng chỉ được đánh giá một lần.");
            }

            // 3. Check if user has booked a ticket and the showtime has ended to mark IsVerifiedViewer
            var now = DateTime.UtcNow;
            var hasValidBooking = await _context.Bookings
                .AsNoTracking()
                .AnyAsync(b => b.UserId == userId &&
                               b.Showtime.MovieId == createDto.MovieId &&
                               (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn") &&
                               b.Showtime.EndTime <= now);

            var review = _mapper.Map<Review>(createDto);
            review.UserId = userId;
            review.LikesCount = 0;
            review.CreatedAt = DateTime.UtcNow;
            review.IsVerifiedViewer = hasValidBooking;
            review.IsApproved = true;
            review.Status = "Approved";

            await _reviewRepository.AddReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            // Load complete review including navigation properties for mapped response
            var savedReview = await _reviewRepository.GetReviewByIdAsync(review.ReviewId);
            return _mapper.Map<ReviewDto>(savedReview);
        }

        public async Task<ReviewDto?> UpdateReviewAsync(int id, int userId, ReviewUpdateDto updateDto)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            if (review == null) return null;

            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa đánh giá này.");
            }

            _mapper.Map(updateDto, review);
            await _reviewRepository.UpdateReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            var updatedReview = await _reviewRepository.GetReviewByIdAsync(id);
            return _mapper.Map<ReviewDto>(updatedReview);
        }

        public async Task<bool> DeleteReviewAsync(int id, int userId, string userRole)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            if (review == null) return false;

            // Allow only the author or Admin to delete the review
            if (review.UserId != userId && userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa đánh giá này.");
            }

            // Hard-delete all associated replies
            var replies = await _context.ReviewReplies
                .Where(rr => rr.ReviewId == id)
                .ToListAsync();
            _context.ReviewReplies.RemoveRange(replies);

            // Hard-delete all associated likes & dislikes
            var likes = await _context.ReviewLikes.Where(rl => rl.ReviewId == id).ToListAsync();
            _context.ReviewLikes.RemoveRange(likes);

            var dislikes = await _context.ReviewDislikes.Where(rd => rd.ReviewId == id).ToListAsync();
            _context.ReviewDislikes.RemoveRange(dislikes);

            await _reviewRepository.DeleteReviewAsync(review);
            return await _reviewRepository.SaveChangesAsync();
        }

        public async Task<ReviewDto?> LikeReviewAsync(int id, int userId)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            if (review == null) return null;

            var existingDislike = await _context.ReviewDislikes
                .FirstOrDefaultAsync(rd => rd.ReviewId == id && rd.UserId == userId);
            if (existingDislike != null)
            {
                _context.ReviewDislikes.Remove(existingDislike);
                review.DislikesCount = Math.Max(0, review.DislikesCount - 1);
            }

            var existingLike = await _context.ReviewLikes
                .FirstOrDefaultAsync(rl => rl.ReviewId == id && rl.UserId == userId);

            if (existingLike != null)
            {
                _context.ReviewLikes.Remove(existingLike);
                review.LikesCount = Math.Max(0, review.LikesCount - 1);
            }
            else
            {
                var reviewLike = new ReviewLike
                {
                    ReviewId = id,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                };
                await _context.ReviewLikes.AddAsync(reviewLike);
                review.LikesCount++;
            }

            await _reviewRepository.UpdateReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            await _context.Entry(review).Collection(r => r.Replies).Query().Include(rp => rp.User).LoadAsync();
            return _mapper.Map<ReviewDto>(review);
        }

        public async Task<ReviewDto?> DislikeReviewAsync(int id, int userId)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            if (review == null) return null;

            var existingLike = await _context.ReviewLikes
                .FirstOrDefaultAsync(rl => rl.ReviewId == id && rl.UserId == userId);
            if (existingLike != null)
            {
                _context.ReviewLikes.Remove(existingLike);
                review.LikesCount = Math.Max(0, review.LikesCount - 1);
            }

            var existingDislike = await _context.ReviewDislikes
                .FirstOrDefaultAsync(rd => rd.ReviewId == id && rd.UserId == userId);

            if (existingDislike != null)
            {
                _context.ReviewDislikes.Remove(existingDislike);
                review.DislikesCount = Math.Max(0, review.DislikesCount - 1);
            }
            else
            {
                var reviewDislike = new ReviewDislike
                {
                    ReviewId = id,
                    UserId = userId,
                    DislikedAt = DateTime.UtcNow
                };
                await _context.ReviewDislikes.AddAsync(reviewDislike);
                review.DislikesCount++;
            }

            await _reviewRepository.UpdateReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            await _context.Entry(review).Collection(r => r.Replies).Query().Include(rp => rp.User).LoadAsync();
            return _mapper.Map<ReviewDto>(review);
        }

        public async Task<ReviewReplyDto?> AddReplyAsync(int reviewId, int userId, ReviewReplyCreateDto createDto)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(reviewId);
            if (review == null) return null;

            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                UserId = userId,
                Content = createDto.Content,
                ParentReplyId = createDto.ParentReplyId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ReviewReplies.AddAsync(reply);
            await _context.SaveChangesAsync();

            await _context.Entry(reply).Reference(r => r.User).LoadAsync();
            if (reply.ParentReplyId.HasValue)
            {
                await _context.Entry(reply).Reference(r => r.ParentReply).LoadAsync();
                if (reply.ParentReply != null)
                {
                    await _context.Entry(reply.ParentReply).Reference(pr => pr.User).LoadAsync();
                }
            }

            return _mapper.Map<ReviewReplyDto>(reply);
        }

        public async Task<bool> DeleteReplyAsync(int replyId, int userId, string userRole)
        {
            var reply = await _context.ReviewReplies.FirstOrDefaultAsync(rr => rr.ReviewReplyId == replyId);
            if (reply == null) return false;

            if (reply.UserId != userId && userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa phản hồi này.");
            }

            // Recursively hard-delete all descendant replies
            await HardDeleteChildRepliesAsync(replyId);

            _context.ReviewReplies.Remove(reply);
            return await _context.SaveChangesAsync() > 0;
        }

        private async Task HardDeleteChildRepliesAsync(int parentReplyId)
        {
            var children = await _context.ReviewReplies
                .Where(rr => rr.ParentReplyId == parentReplyId)
                .ToListAsync();

            foreach (var child in children)
            {
                await HardDeleteChildRepliesAsync(child.ReviewReplyId);
                _context.ReviewReplies.Remove(child);
            }
        }

        public async Task<double> GetAverageRatingAsync(int movieId)
        {
            return await _reviewRepository.GetAverageRatingAsync(movieId);
        }

        public async Task<MovieRatingSummaryDto> GetMovieRatingSummaryAsync(int movieId)
        {
            return await _reviewRepository.GetMovieRatingSummaryAsync(movieId);
        }

        public async Task<IEnumerable<MovieDto>> GetTopRatedMoviesAsync(int limit)
        {
            var movies = await _reviewRepository.GetTopRatedMoviesAsync(limit);
            return _mapper.Map<IEnumerable<MovieDto>>(movies);
        }
        public async Task<CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>> GetPagedReviewsAsync(ReviewQueryParameters queryParams)
        {
            var (reviews, totalCount) = await _reviewRepository.GetPagedReviewsAsync(queryParams);
            var dtos = _mapper.Map<List<ReviewDto>>(reviews);

            return new CinemaBooking.API.DTOs.Common.PagedResultDto<ReviewDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ReviewDto?> UpdateReviewStatusAsync(int id, string status)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            if (review == null) return null;

            review.Status = status;
            review.IsApproved = (status == "Approved");

            if (status == "Deleted")
            {
                review.IsDeleted = true;
                review.DeletedAt = DateTime.UtcNow;
            }
            else
            {
                review.IsDeleted = false;
                review.DeletedAt = null;
            }

            review.UpdatedAt = DateTime.UtcNow;
            await _reviewRepository.UpdateReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            return _mapper.Map<ReviewDto>(review);
        }

        public async Task<ReviewAnalyticsDto> GetReviewAnalyticsAsync()
        {
            return await _reviewRepository.GetReviewAnalyticsAsync();
        }
    }
}
