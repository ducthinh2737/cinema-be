using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IMapper _mapper;

        public MovieService(IMovieRepository movieRepository, IMapper mapper)
        {
            _movieRepository = movieRepository;
            _mapper = mapper;
        }

        public async Task<PagedResultDto<MovieDto>> GetPagedMoviesAsync(MovieQueryParameters queryParams)
        {
            var (items, totalCount) = await _movieRepository.GetPagedMoviesAsync(queryParams);

            return new PagedResultDto<MovieDto>
            {
                Items = _mapper.Map<List<MovieDto>>(items),
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<MovieDetailDto?> GetMovieByIdAsync(int id)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            if (movie == null) return null;

            return _mapper.Map<MovieDetailDto>(movie);
        }

        public async Task<MovieDetailDto?> GetMovieBySlugAsync(string slug)
        {
            var movie = await _movieRepository.GetBySlugAsync(slug);
            if (movie == null) return null;

            return _mapper.Map<MovieDetailDto>(movie);
        }

        public async Task<MovieDetailDto> CreateMovieAsync(MovieCreateDto createDto)
        {
            var movie = _mapper.Map<Movie>(createDto);

            // Generate unique slug
            string baseSlug = GenerateSlug(createDto.Title);
            string slug = baseSlug;
            int counter = 1;
            while (await _movieRepository.GetBySlugAsync(slug) != null)
            {
                slug = $"{baseSlug}-{counter++}";
            }
            movie.Slug = slug;
            movie.Rating = 0.0;
            movie.CreatedAt = DateTime.UtcNow;
            movie.CreatedBy = "Admin";
            movie.IsDeleted = false;

            // Map Actors
            if (createDto.ActorIds != null && createDto.ActorIds.Any())
            {
                foreach (var actorId in createDto.ActorIds)
                {
                    movie.MovieActors.Add(new MovieActor { ActorId = actorId });
                }
            }

            await _movieRepository.AddAsync(movie);
            await _movieRepository.SaveChangesAsync();

            // Fetch fully populated movie to return complete detail
            var savedMovie = await _movieRepository.GetByIdWithDetailsAsync(movie.Id);
            return _mapper.Map<MovieDetailDto>(savedMovie ?? movie);
        }

        public async Task<MovieDetailDto?> UpdateMovieAsync(int id, MovieUpdateDto updateDto)
        {
            var movie = await _movieRepository.GetByIdWithDetailsAsync(id);
            if (movie == null) return null;

            // Update slug if Title changed
            if (!movie.Title.Equals(updateDto.Title, StringComparison.OrdinalIgnoreCase))
            {
                string baseSlug = GenerateSlug(updateDto.Title);
                string slug = baseSlug;
                int counter = 1;
                while (await _movieRepository.GetBySlugAsync(slug) != null)
                {
                    slug = $"{baseSlug}-{counter++}";
                }
                movie.Slug = slug;
            }

            // Map standard properties
            _mapper.Map(updateDto, movie);
            movie.LastModifiedAt = DateTime.UtcNow;
            movie.LastModifiedBy = "Admin";

            // Update Actors list (clear and re-populate)
            movie.MovieActors.Clear();
            if (updateDto.ActorIds != null)
            {
                foreach (var actorId in updateDto.ActorIds)
                {
                    movie.MovieActors.Add(new MovieActor { ActorId = actorId });
                }
            }

            _movieRepository.Update(movie);
            await _movieRepository.SaveChangesAsync();

            // Return updated details
            var updatedMovie = await _movieRepository.GetByIdWithDetailsAsync(id);
            return _mapper.Map<MovieDetailDto>(updatedMovie ?? movie);
        }

        public async Task<bool> DeleteMovieAsync(int id, string deletedBy)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null) return false;

            // Perform Soft Delete
            movie.IsDeleted = true;
            movie.DeletedAt = DateTime.UtcNow;
            movie.DeletedBy = deletedBy;

            _movieRepository.Update(movie);
            return await _movieRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdateMoviePhotosAsync(int id, string? posterUrl, string? bannerUrl)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null) return false;

            if (posterUrl != null) movie.PosterUrl = posterUrl;
            if (bannerUrl != null) movie.BannerUrl = bannerUrl;

            movie.LastModifiedAt = DateTime.UtcNow;
            movie.LastModifiedBy = "Admin";

            _movieRepository.Update(movie);
            return await _movieRepository.SaveChangesAsync();
        }

        #region Helper Methods for Slug Generation

        private string GenerateSlug(string title)
        {
            string cleanTitle = RemoveAccents(title).ToLowerInvariant();
            
            // Remove invalid characters
            cleanTitle = Regex.Replace(cleanTitle, @"[^a-z0-9\s-]", "");
            
            // Collapse multiple spaces into one
            cleanTitle = Regex.Replace(cleanTitle, @"\s+", " ").Trim();
            
            // Limit length to 45 chars
            cleanTitle = cleanTitle.Substring(0, Math.Min(cleanTitle.Length, 45)).Trim();
            
            // Replace spaces with hyphens
            cleanTitle = Regex.Replace(cleanTitle, @"\s", "-");
            
            return cleanTitle;
        }

        private string RemoveAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        #endregion
    }
}
