using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Reviews;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public ReviewService(IReviewRepository reviewRepository, IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _mapper = mapper;
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
            var existing = await _reviewRepository.GetReviewByUserAndMovieAsync(userId, createDto.MovieId);
            if (existing != null)
            {
                throw new InvalidOperationException("You have already reviewed this movie. A user can only review each movie once.");
            }

            var review = _mapper.Map<Review>(createDto);
            review.UserId = userId;
            review.LikesCount = 0;

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
                throw new UnauthorizedAccessException("You do not have permission to update this review.");
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
                throw new UnauthorizedAccessException("You do not have permission to delete this review.");
            }

            await _reviewRepository.DeleteReviewAsync(review);
            return await _reviewRepository.SaveChangesAsync();
        }

        public async Task<ReviewDto?> LikeReviewAsync(int id)
        {
            var review = await _reviewRepository.GetReviewByIdAsync(id);
            if (review == null) return null;

            review.LikesCount++;
            await _reviewRepository.UpdateReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            return _mapper.Map<ReviewDto>(review);
        }

        public async Task<double> GetAverageRatingAsync(int movieId)
        {
            return await _reviewRepository.GetAverageRatingAsync(movieId);
        }

        public async Task<IEnumerable<MovieDto>> GetTopRatedMoviesAsync(int limit)
        {
            var movies = await _reviewRepository.GetTopRatedMoviesAsync(limit);
            return _mapper.Map<IEnumerable<MovieDto>>(movies);
        }
    }
}
