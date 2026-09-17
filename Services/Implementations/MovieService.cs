using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;
using CinemaBooking.API.SignalR;

namespace CinemaBooking.API.Services.Implementations
{
    /// <summary>
    /// Enterprise Movie Management Service handling movie lifecycle, 
    /// unique SEO slug creation, validation, transaction security, 
    /// optimized actor mapping, soft-delete rules, and performance analytics.
    /// </summary>
    public class MovieService : IMovieService
    {
        private readonly CinemaDbContext _context;
        private readonly IMovieRepository _movieRepository;
        private readonly ISlugService _slugService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<MovieService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public MovieService(
            CinemaDbContext context,
            IMovieRepository movieRepository,
            ISlugService slugService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<MovieService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _movieRepository = movieRepository;
            _slugService = slugService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #region Query Methods

        /// <summary>
        /// Retrieves a paginated list of movies based on search terms, genres, years, and status.
        /// </summary>
        public async Task<ApiResponse<PagedResultDto<MovieDto>>> GetPagedMoviesAsync(MovieQueryParameters queryParams)
        {
            _logger.LogInformation("Retrieving paginated movies. Page: {Page}", queryParams.PageNumber);
            var now = DateTime.UtcNow;

            var query = _context.Movies
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .Include(m => m.MovieFormats)
                .AsSplitQuery()
                .AsNoTracking();

            // Handle deleted movies filter
            if (!queryParams.IncludeDeleted)
            {
                query = query.Where(m => !m.IsDeleted);
            }
            else
            {
                query = query.IgnoreQueryFilters();
            }

            // Search by Title
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var search = queryParams.SearchTerm.ToLower();
                query = query.Where(m => m.Title.ToLower().Contains(search));
            }

            // Filter by Genre
            if (queryParams.GenreId.HasValue)
            {
                query = query.Where(m => m.GenreId == queryParams.GenreId.Value);
            }

            // Filter by Release Year
            if (queryParams.ReleaseYear.HasValue)
            {
                query = query.Where(m => m.ReleaseDate.Year == queryParams.ReleaseYear.Value);
            }

            // Filter by Status
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                var statusLower = queryParams.Status.ToLower();
                query = statusLower switch
                {
                    "comingsoon" => query.Where(m => m.Status != "Hidden" && m.ReleaseDate > now),
                    "nowshowing" => query.Where(m => m.Status != "Hidden" && m.ReleaseDate <= now && m.EndDate >= now),
                    "ended" => query.Where(m => m.Status != "Hidden" && m.EndDate < now),
                    "hidden" => query.IgnoreQueryFilters().Where(m => m.IsDeleted || m.Status == "Hidden"),
                    "special" => query.Where(m => m.ReleaseDate > now && m.Showtimes.Any(s => s.StartTime >= now)),
                    _ => query
                };
            }

            // Sorting
            if (!string.IsNullOrEmpty(queryParams.SortBy))
            {
                query = queryParams.SortBy.ToLower() switch
                {
                    "title" => queryParams.IsDescending ? query.OrderByDescending(m => m.Title) : query.OrderBy(m => m.Title),
                    "releasedate" => queryParams.IsDescending ? query.OrderByDescending(m => m.ReleaseDate) : query.OrderBy(m => m.ReleaseDate),
                    "rating" => queryParams.IsDescending ? query.OrderByDescending(m => m.Rating) : query.OrderBy(m => m.Rating),
                    "duration" => queryParams.IsDescending ? query.OrderByDescending(m => m.Duration) : query.OrderBy(m => m.Duration),
                    _ => queryParams.IsDescending ? query.OrderByDescending(m => m.Id) : query.OrderBy(m => m.Id)
                };
            }
            else
            {
                query = query.OrderByDescending(m => m.ReleaseDate);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<MovieDto>>(items);

            foreach (var dto in dtos)
            {
                var movieItem = items.First(x => x.Id == dto.Id);
                if (movieItem.IsDeleted || movieItem.Status == "Hidden")
                {
                    dto.Status = "Hidden";
                }
                else if (movieItem.ReleaseDate > now)
                {
                    dto.Status = "ComingSoon";
                }
                else if (movieItem.EndDate < now)
                {
                    dto.Status = "Ended";
                }
                else
                {
                    dto.Status = "NowShowing";
                }
            }

            var pagedResult = new PagedResultDto<MovieDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };

            return ApiResponse.Success(pagedResult);
        }

        /// <summary>
        /// Gets full movie details by ID, including actors and dynamic analytics.
        /// </summary>
        public async Task<ApiResponse<MovieDetailDto>> GetMovieByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving movie details by ID: {MovieId}", id);

            var movie = await _context.Movies
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                .Include(m => m.MovieFormats)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                throw new NotFoundException($"Không tìm thấy phim có ID {id}.");
            }

            var dto = _mapper.Map<MovieDetailDto>(movie);
            var now = DateTime.UtcNow;
            dto.Status = movie.IsDeleted || movie.Status == "Hidden" ? "Hidden"
                         : movie.ReleaseDate > now ? "ComingSoon"
                         : movie.EndDate < now ? "Ended"
                         : "NowShowing";

            // Populate analytics
            dto.Analytics = await CalculateMovieAnalyticsAsync(id);

            // Populate review rating and summary details
            var summary = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.MovieId == movie.Id && !r.IsDeleted && r.Status == "Approved")
                .GroupBy(r => r.MovieId)
                .Select(g => new CinemaBooking.API.DTOs.Reviews.MovieRatingSummaryDto
                {
                    TotalReviews = g.Count(),
                    AverageRating = g.Average(r => (double)r.Rating),
                    FiveStarCount = g.Count(r => r.Rating == 5),
                    FourStarCount = g.Count(r => r.Rating == 4),
                    ThreeStarCount = g.Count(r => r.Rating == 3),
                    TwoStarCount = g.Count(r => r.Rating == 2),
                    OneStarCount = g.Count(r => r.Rating == 1)
                })
                .FirstOrDefaultAsync();

            if (summary != null)
            {
                summary.AverageRating = Math.Round(summary.AverageRating, 1, MidpointRounding.AwayFromZero);
                dto.AverageRating = summary.AverageRating;
                dto.ReviewCount = summary.TotalReviews;
                dto.RatingSummary = summary;
            }
            else
            {
                dto.AverageRating = 0.0;
                dto.ReviewCount = 0;
                dto.RatingSummary = new CinemaBooking.API.DTOs.Reviews.MovieRatingSummaryDto
                {
                    AverageRating = 0.0,
                    TotalReviews = 0,
                    FiveStarCount = 0,
                    FourStarCount = 0,
                    ThreeStarCount = 0,
                    TwoStarCount = 0,
                    OneStarCount = 0
                };
            }

            return ApiResponse.Success(dto);
        }

        /// <summary>
        /// Gets full movie details by slug, including actors and dynamic analytics.
        /// </summary>
        public async Task<ApiResponse<MovieDetailDto>> GetMovieBySlugAsync(string slug)
        {
            _logger.LogInformation("Retrieving movie details by Slug: {Slug}", slug);

            var movie = await _context.Movies
                .Include(m => m.Genre)
                .Include(m => m.Director)
                .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                .Include(m => m.MovieFormats)
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (movie == null)
            {
                throw new NotFoundException($"Không tìm thấy phim có slug '{slug}'.");
            }

            var dto = _mapper.Map<MovieDetailDto>(movie);
            var now = DateTime.UtcNow;
            dto.Status = movie.IsDeleted || movie.Status == "Hidden" ? "Hidden"
                         : movie.ReleaseDate > now ? "ComingSoon"
                         : movie.EndDate < now ? "Ended"
                         : "NowShowing";

            // Populate analytics
            dto.Analytics = await CalculateMovieAnalyticsAsync(movie.Id);

            // Populate review rating and summary details
            var summary = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.MovieId == movie.Id && !r.IsDeleted && r.Status == "Approved")
                .GroupBy(r => r.MovieId)
                .Select(g => new CinemaBooking.API.DTOs.Reviews.MovieRatingSummaryDto
                {
                    TotalReviews = g.Count(),
                    AverageRating = g.Average(r => (double)r.Rating),
                    FiveStarCount = g.Count(r => r.Rating == 5),
                    FourStarCount = g.Count(r => r.Rating == 4),
                    ThreeStarCount = g.Count(r => r.Rating == 3),
                    TwoStarCount = g.Count(r => r.Rating == 2),
                    OneStarCount = g.Count(r => r.Rating == 1)
                })
                .FirstOrDefaultAsync();

            if (summary != null)
            {
                summary.AverageRating = Math.Round(summary.AverageRating, 1, MidpointRounding.AwayFromZero);
                dto.AverageRating = summary.AverageRating;
                dto.ReviewCount = summary.TotalReviews;
                dto.RatingSummary = summary;
            }
            else
            {
                dto.AverageRating = 0.0;
                dto.ReviewCount = 0;
                dto.RatingSummary = new CinemaBooking.API.DTOs.Reviews.MovieRatingSummaryDto
                {
                    AverageRating = 0.0,
                    TotalReviews = 0,
                    FiveStarCount = 0,
                    FourStarCount = 0,
                    ThreeStarCount = 0,
                    TwoStarCount = 0,
                    OneStarCount = 0
                };
            }

            return ApiResponse.Success(dto);
        }

        #endregion

        #region Mutation Methods

        /// <summary>
        /// Creates a new movie entity, generates its unique slug, updates actors, and broadcasts.
        /// </summary>
        public async Task<ApiResponse<MovieDetailDto>> CreateMovieAsync(MovieCreateDto createDto)
        {
            _logger.LogInformation("Creating movie: {Title}", createDto.Title);

            await ValidateMovieAsync(createDto);

            var currentUsername = _currentUserService.UserName ?? "System";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var movie = _mapper.Map<Movie>(createDto);
                movie.Slug = await _slugService.GenerateUniqueSlugAsync<Movie>(createDto.Title);
                movie.Rating = 0.0;
                movie.CreatedAt = DateTime.UtcNow;
                movie.CreatedBy = currentUsername;
                movie.IsDeleted = false;

                // Process director string if provided
                if (!string.IsNullOrWhiteSpace(createDto.Director))
                {
                    var directorName = createDto.Director.Trim();
                    var director = await _context.Directors
                        .FirstOrDefaultAsync(d => d.FullName.ToLower() == directorName.ToLower());
                    if (director == null)
                    {
                        director = new Director { FullName = directorName };
                        await _context.Directors.AddAsync(director);
                        await _context.SaveChangesAsync();
                    }
                    movie.DirectorId = director.DirectorId;
                }

                // Add actors relationship
                var mergedActorIds = new List<int>(createDto.ActorIds ?? new List<int>());
                if (createDto.Actors != null && createDto.Actors.Any())
                {
                    foreach (var actorNameRaw in createDto.Actors)
                    {
                        var actorName = actorNameRaw.Trim();
                        if (string.IsNullOrEmpty(actorName)) continue;

                        var actor = await _context.Actors
                            .FirstOrDefaultAsync(a => a.FullName.ToLower() == actorName.ToLower());
                        if (actor == null)
                        {
                            actor = new Actor { FullName = actorName };
                            await _context.Actors.AddAsync(actor);
                            await _context.SaveChangesAsync();
                        }
                        if (!mergedActorIds.Contains(actor.ActorId))
                        {
                            mergedActorIds.Add(actor.ActorId);
                        }
                    }
                }

                if (mergedActorIds.Any())
                {
                    foreach (var actorId in mergedActorIds)
                    {
                        movie.MovieActors.Add(new MovieActor { ActorId = actorId });
                    }
                }

                // Add formats relationship
                if (createDto.MovieFormatIds != null && createDto.MovieFormatIds.Any())
                {
                    var formats = await _context.MovieFormats
                        .Where(f => createDto.MovieFormatIds.Contains(f.MovieFormatId))
                        .ToListAsync();
                    foreach (var fmt in formats)
                    {
                        movie.MovieFormats.Add(fmt);
                    }
                }

                await _context.Movies.AddAsync(movie);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Movie '{Title}' created successfully with ID {MovieId}", movie.Title, movie.Id);

                // Load detail
                var savedDetail = await GetMovieByIdAsync(movie.Id);

                // Broadcast SignalR creation event
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("MovieCreated", savedDetail.Data);
                }

                return savedDetail;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during movie creation.");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing movie entity with collision safety and transaction guarantees.
        /// </summary>
        public async Task<ApiResponse<MovieDetailDto>> UpdateMovieAsync(int id, MovieUpdateDto updateDto)
        {
            _logger.LogInformation("Updating movie ID {MovieId}", id);

            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                .Include(m => m.MovieFormats)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                throw new NotFoundException($"Không tìm thấy phim có ID {id} để cập nhật.");
            }

            // Check if Title is changing to generate a new slug
            string? newSlug = null;
            if (!movie.Title.Equals(updateDto.Title, StringComparison.OrdinalIgnoreCase))
            {
                newSlug = await _slugService.GenerateUniqueSlugAsync<Movie>(updateDto.Title);
            }

            var currentUsername = _currentUserService.UserName ?? "System";

            // Validate using standard create dto validation
            var createDto = new MovieCreateDto
            {
                Title = updateDto.Title,
                Description = updateDto.Description,
                Duration = updateDto.Duration,
                Language = updateDto.Language,
                TrailerUrl = updateDto.TrailerUrl,
                ReleaseDate = updateDto.ReleaseDate,
                EndDate = updateDto.EndDate,
                GenreId = updateDto.GenreId,
                AgeRatingId = updateDto.AgeRatingId,
                DirectorId = updateDto.DirectorId,
                Director = updateDto.Director,
                IsFeatured = updateDto.IsFeatured,
                Status = updateDto.Status,
                ActorIds = updateDto.ActorIds,
                Actors = updateDto.Actors,
                MovieFormatIds = updateDto.MovieFormatIds
            };
            await ValidateMovieAsync(createDto, id);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _mapper.Map(updateDto, movie);
                if (newSlug != null)
                {
                    movie.Slug = newSlug;
                }
                movie.LastModifiedAt = DateTime.UtcNow;
                movie.LastModifiedBy = currentUsername;

                // Process director string if provided
                if (!string.IsNullOrWhiteSpace(updateDto.Director))
                {
                    var directorName = updateDto.Director.Trim();
                    var director = await _context.Directors
                        .FirstOrDefaultAsync(d => d.FullName.ToLower() == directorName.ToLower());
                    if (director == null)
                    {
                        director = new Director { FullName = directorName };
                        await _context.Directors.AddAsync(director);
                        await _context.SaveChangesAsync();
                    }
                    movie.DirectorId = director.DirectorId;
                }
                else
                {
                    movie.DirectorId = updateDto.DirectorId;
                }

                // Process actors
                var mergedActorIds = new List<int>(updateDto.ActorIds ?? new List<int>());
                if (updateDto.Actors != null && updateDto.Actors.Any())
                {
                    foreach (var actorNameRaw in updateDto.Actors)
                    {
                        var actorName = actorNameRaw.Trim();
                        if (string.IsNullOrEmpty(actorName)) continue;

                        var actor = await _context.Actors
                            .FirstOrDefaultAsync(a => a.FullName.ToLower() == actorName.ToLower());
                        if (actor == null)
                        {
                            actor = new Actor { FullName = actorName };
                            await _context.Actors.AddAsync(actor);
                            await _context.SaveChangesAsync();
                        }
                        if (!mergedActorIds.Contains(actor.ActorId))
                        {
                            mergedActorIds.Add(actor.ActorId);
                        }
                    }
                }

                // Optimized Actor Update (Compare difference instead of Clear & Add)
                await UpdateMovieActorsInternalAsync(movie, mergedActorIds);

                // Optimized Format Update
                await UpdateMovieFormatsInternalAsync(movie, updateDto.MovieFormatIds);

                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Movie ID {MovieId} updated successfully.", id);

                var savedDetail = await GetMovieByIdAsync(id);

                // Broadcast SignalR update event
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("MovieUpdated", savedDetail.Data);
                }

                return savedDetail;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during movie update.");
                throw;
            }
        }

        /// <summary>
        /// Performs a soft delete on a movie. Throws business exception if movie has active showtimes.
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteMovieAsync(int id)
        {
            _logger.LogInformation("Soft-deleting movie ID {MovieId}", id);

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null || movie.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy phim có ID {id} hoặc phim đã bị ẩn trước đó.");
            }

            // Business rule: Prevent deletion if movie has future or active showtimes
            var hasActiveShowtimes = await _context.Showtimes
                .AnyAsync(s => s.MovieId == id && s.EndTime > DateTime.UtcNow);
            if (hasActiveShowtimes)
            {
                throw new BusinessException("Không thể xóa phim này vì hiện tại đang có lịch chiếu hoạt động trong tương lai.");
            }

            var currentUsername = _currentUserService.UserName ?? "System";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                movie.IsDeleted = true;
                movie.DeletedAt = DateTime.UtcNow;
                movie.DeletedBy = currentUsername;

                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Movie ID {MovieId} has been successfully soft-deleted.", id);

                // Broadcast SignalR deletion event
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("MovieDeleted", id);
                }

                return ApiResponse.Success(true, "Xóa phim thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during movie deletion.");
                throw;
            }
        }

        /// <summary>
        /// Restores a soft-deleted movie back to active status.
        /// </summary>
        public async Task<ApiResponse<bool>> RestoreMovieAsync(int id)
        {
            _logger.LogInformation("Restoring movie ID {MovieId}", id);

            // Fetch bypassing global soft-delete query filter
            var movie = await _context.Movies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null || !movie.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy phim bị ẩn có ID {id}.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                movie.IsDeleted = false;
                movie.DeletedAt = null;
                movie.DeletedBy = null;
                movie.LastModifiedAt = DateTime.UtcNow;
                movie.LastModifiedBy = _currentUserService.UserName ?? "System";

                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Movie ID {MovieId} successfully restored.", id);

                // Broadcast SignalR restore event
                var savedDetail = await GetMovieByIdAsync(id);
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("MovieCreated", savedDetail.Data);
                }

                return ApiResponse.Success(true, "Phục hồi phim thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during movie restoration.");
                throw;
            }
        }

        /// <summary>
        /// Updates the image paths (poster and/or banner) of the movie.
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateMoviePhotosAsync(int id, string? posterUrl, string? bannerUrl)
        {
            _logger.LogInformation("Updating photos for movie ID {MovieId}", id);

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                throw new NotFoundException($"Không tìm thấy phim có ID {id} để cập nhật hình ảnh.");
            }

            if (posterUrl != null)
            {
                if (!IsValidUrlOrPath(posterUrl))
                    throw new ValidationException("Đường dẫn ảnh Poster không hợp lệ.");
                movie.PosterUrl = posterUrl;
            }

            if (bannerUrl != null)
            {
                if (!IsValidUrlOrPath(bannerUrl))
                    throw new ValidationException("Đường dẫn ảnh Banner không hợp lệ.");
                movie.BannerUrl = bannerUrl;
            }

            movie.LastModifiedAt = DateTime.UtcNow;
            movie.LastModifiedBy = _currentUserService.UserName ?? "System";

            _context.Movies.Update(movie);
            var success = await _context.SaveChangesAsync() > 0;

            if (success)
            {
                // Broadcast change
                var savedDetail = await GetMovieByIdAsync(id);
                var hubContext = _serviceProvider.GetService<IHubContext<SeatHub>>();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("MovieUpdated", savedDetail.Data);
                }
            }

            return ApiResponse.Success(success, "Cập nhật ảnh thành công.");
        }

        /// <summary>
        /// Updates movie-actor relationships using optimized diff comparisons.
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateMovieActorsAsync(int id, List<int> actorIds)
        {
            _logger.LogInformation("Updating actor list for movie ID {MovieId}", id);

            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                throw new NotFoundException($"Không tìm thấy phim có ID {id}.");
            }

            // Verify all new actorIds exist
            var existingActorsCount = await _context.Actors
                .Where(a => actorIds.Contains(a.ActorId))
                .CountAsync();

            if (existingActorsCount != actorIds.Distinct().Count())
            {
                throw new ValidationException("Một hoặc nhiều Actor ID được truyền lên không tồn tại trong hệ thống.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await UpdateMovieActorsInternalAsync(movie, actorIds);
                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ApiResponse.Success(true, "Cập nhật danh sách diễn viên thành công.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Helper Validation & Analysis Systems

        /// <summary>
        /// Validates movie details against core business rules.
        /// </summary>
        public async Task<ApiResponse<bool>> ValidateMovieAsync(MovieCreateDto createDto)
        {
            return await ValidateMovieAsync(createDto, null);
        }

        private async Task<ApiResponse<bool>> ValidateMovieAsync(MovieCreateDto createDto, int? excludeMovieId)
        {
            if (string.IsNullOrWhiteSpace(createDto.Title))
            {
                throw new ValidationException("Tiêu đề phim bắt buộc không được để trống.");
            }

            if (createDto.Duration <= 0)
            {
                throw new ValidationException("Thời lượng phim phải lớn hơn 0 phút.");
            }

            if (createDto.ReleaseDate >= createDto.EndDate)
            {
                throw new ValidationException("Ngày phát hành (ReleaseDate) phải trước ngày kết thúc chiếu (EndDate).");
            }

            // Unique title validation
            var isDuplicate = await _context.Movies
                .AnyAsync(m => m.Title.ToLower() == createDto.Title.ToLower() && m.Id != excludeMovieId && !m.IsDeleted);

            if (isDuplicate)
            {
                throw new BusinessException($"Phim có tiêu đề '{createDto.Title}' đã tồn tại trong hệ thống.");
            }

            // Genre validation
            var genreExists = await _context.Genres.AnyAsync(g => g.GenreId == createDto.GenreId);
            if (!genreExists)
            {
                throw new ValidationException("Thể loại phim (Genre) được chọn không tồn tại.");
            }

            // Actors validation
            if (createDto.ActorIds != null && createDto.ActorIds.Any())
            {
                var activeActorsCount = await _context.Actors
                    .Where(a => createDto.ActorIds.Contains(a.ActorId))
                    .CountAsync();

                if (activeActorsCount != createDto.ActorIds.Distinct().Count())
                {
                    throw new ValidationException("Một hoặc nhiều diễn viên được chọn không hợp lệ.");
                }
            }

            // Movie formats validation
            if (createDto.MovieFormatIds != null && createDto.MovieFormatIds.Any())
            {
                var activeFormatsCount = await _context.MovieFormats
                    .Where(f => createDto.MovieFormatIds.Contains(f.MovieFormatId) && !f.IsDeleted)
                    .CountAsync();

                if (activeFormatsCount != createDto.MovieFormatIds.Distinct().Count())
                {
                    throw new ValidationException("Một hoặc nhiều định dạng phim được chọn không hợp lệ hoặc đã bị xóa.");
                }
            }

            return ApiResponse.Success(true);
        }

        private async Task UpdateMovieActorsInternalAsync(Movie movie, List<int> targetActorIds)
        {
            var currentActorIds = movie.MovieActors.Select(ma => ma.ActorId).ToList();
            var toAdd = targetActorIds.Except(currentActorIds).ToList();
            var toRemove = currentActorIds.Except(targetActorIds).ToList();

            foreach (var removeId in toRemove)
            {
                var ma = movie.MovieActors.First(x => x.ActorId == removeId);
                movie.MovieActors.Remove(ma);
            }

            foreach (var addId in toAdd)
            {
                movie.MovieActors.Add(new MovieActor { ActorId = addId });
            }

            await Task.CompletedTask;
        }

        private async Task UpdateMovieFormatsInternalAsync(Movie movie, List<int> targetFormatIds)
        {
            var currentFormatIds = movie.MovieFormats.Select(f => f.MovieFormatId).ToList();
            var toAdd = targetFormatIds.Except(currentFormatIds).ToList();
            var toRemove = currentFormatIds.Except(targetFormatIds).ToList();

            foreach (var removeId in toRemove)
            {
                var fmt = movie.MovieFormats.First(x => x.MovieFormatId == removeId);
                movie.MovieFormats.Remove(fmt);
            }

            if (toAdd.Any())
            {
                var formatsToAdd = await _context.MovieFormats
                    .Where(f => toAdd.Contains(f.MovieFormatId))
                    .ToListAsync();
                foreach (var fmt in formatsToAdd)
                {
                    movie.MovieFormats.Add(fmt);
                }
            }
        }

        private async Task<MovieAnalyticsDto> CalculateMovieAnalyticsAsync(int movieId)
        {
            var analytics = new MovieAnalyticsDto
            {
                MovieId = movieId,
                Title = string.Empty,
                BookingCount = 0,
                Revenue = 0m,
                RatingAverage = 0.0,
                OccupancyRate = 0.0
            };

            var movie = await _context.Movies.FindAsync(movieId);
            if (movie != null)
            {
                analytics.Title = movie.Title;

                // Total bookings
                analytics.BookingCount = await _context.BookingSeats
                    .CountAsync(bs => bs.Booking.Showtime.MovieId == movieId && (bs.Booking.BookingStatus == "Confirmed" || bs.Booking.BookingStatus == "Paid" || bs.Booking.BookingStatus == "CheckedIn"));

                // Total revenue
                analytics.Revenue = await _context.Bookings
                    .Where(b => b.Showtime.MovieId == movieId && (b.BookingStatus == "Confirmed" || b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn"))
                    .SumAsync(b => b.TotalAmount);

                // Rating average
                var reviews = await _context.Reviews.Where(r => r.MovieId == movieId).ToListAsync();
                analytics.RatingAverage = reviews.Any() ? reviews.Average(r => r.Rating) : 0.0;

                // Occupancy rate calculation (booked seats / total capacity across showtimes)
                var showtimes = await _context.Showtimes.Where(s => s.MovieId == movieId).ToListAsync();
                if (showtimes.Any())
                {
                    var totalCapacity = 0;
                    foreach (var s in showtimes)
                    {
                        var seatsCount = await _context.Seats.CountAsync(st => st.HallId == s.HallId && !st.IsDeleted);
                        totalCapacity += seatsCount;
                    }

                    if (totalCapacity > 0)
                    {
                        analytics.OccupancyRate = Math.Round((double)analytics.BookingCount / totalCapacity * 100, 2);
                    }
                }
            }

            return analytics;
        }

        private bool IsValidUrlOrPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return path.StartsWith("/") || Uri.IsWellFormedUriString(path, UriKind.Absolute);
        }

        #endregion
    }
}
