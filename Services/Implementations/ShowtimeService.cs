using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Showtimes;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class ShowtimeService : IShowtimeService
    {
        private readonly IShowtimeRepository _showtimeRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly IMapper _mapper;

        public ShowtimeService(
            IShowtimeRepository showtimeRepository,
            IMovieRepository movieRepository,
            IMapper mapper)
        {
            _showtimeRepository = showtimeRepository;
            _movieRepository = movieRepository;
            _mapper = mapper;
        }

        public async Task<PagedResultDto<ShowtimeDto>> GetPagedShowtimesAsync(ShowtimeQueryParameters queryParams)
        {
            var items = await _showtimeRepository.GetPagedShowtimesAsync(queryParams);
            var totalCount = await _showtimeRepository.GetTotalCountAsync(queryParams);

            var dtos = new List<ShowtimeDto>();
            foreach (var item in items)
            {
                dtos.Add(await MapToShowtimeDtoAsync(item));
            }

            return new PagedResultDto<ShowtimeDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ShowtimeDto?> GetShowtimeByIdAsync(int id)
        {
            var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(id);
            if (showtime == null) return null;

            return await MapToShowtimeDtoAsync(showtime);
        }

        public async Task<IEnumerable<ShowtimeDto>> GetShowtimesByMovieIdAsync(int movieId)
        {
            var items = await _showtimeRepository.GetShowtimesByMovieIdAsync(movieId);
            var dtos = new List<ShowtimeDto>();
            foreach (var item in items)
            {
                dtos.Add(await MapToShowtimeDtoAsync(item));
            }
            return dtos;
        }

        public async Task<IEnumerable<ShowtimeDto>> GetShowtimesByCinemaIdAsync(int cinemaId)
        {
            var items = await _showtimeRepository.GetShowtimesByCinemaIdAsync(cinemaId);
            var dtos = new List<ShowtimeDto>();
            foreach (var item in items)
            {
                dtos.Add(await MapToShowtimeDtoAsync(item));
            }
            return dtos;
        }

        public async Task<ShowtimeDto> CreateShowtimeAsync(ShowtimeCreateDto createDto)
        {
            // Validate Movie and calculate EndTime
            var movie = await _movieRepository.GetByIdAsync(createDto.MovieId);
            if (movie == null)
            {
                throw new ArgumentException($"Movie with ID {createDto.MovieId} does not exist.");
            }

            var endTime = createDto.StartTime.AddMinutes(movie.Duration);

            // Check Scheduling Conflicts in the Selected Hall
            var overlapping = await _showtimeRepository.GetOverlappingShowtimesAsync(createDto.HallId, createDto.StartTime, endTime);
            if (overlapping.Any())
            {
                throw new InvalidOperationException("The hall is already booked for another showtime during this time period.");
            }

            var showtime = _mapper.Map<Showtime>(createDto);
            showtime.EndTime = endTime;

            await _showtimeRepository.AddAsync(showtime);
            await _showtimeRepository.SaveChangesAsync();

            var savedShowtime = await _showtimeRepository.GetByIdWithDetailsAsync(showtime.ShowtimeId);
            return await MapToShowtimeDtoAsync(savedShowtime ?? showtime);
        }

        public async Task<ShowtimeDto?> UpdateShowtimeAsync(int id, ShowtimeUpdateDto updateDto)
        {
            var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(id);
            if (showtime == null) return null;

            // Validate Movie and calculate EndTime
            var movie = await _movieRepository.GetByIdAsync(updateDto.MovieId);
            if (movie == null)
            {
                throw new ArgumentException($"Movie with ID {updateDto.MovieId} does not exist.");
            }

            var endTime = updateDto.StartTime.AddMinutes(movie.Duration);

            // Check Scheduling Conflicts in the Selected Hall, excluding current showtime
            var overlapping = await _showtimeRepository.GetOverlappingShowtimesAsync(updateDto.HallId, updateDto.StartTime, endTime, id);
            if (overlapping.Any())
            {
                throw new InvalidOperationException("The hall is already booked for another showtime during this time period.");
            }

            _mapper.Map(updateDto, showtime);
            showtime.EndTime = endTime;

            _showtimeRepository.Update(showtime);
            await _showtimeRepository.SaveChangesAsync();

            var updatedShowtime = await _showtimeRepository.GetByIdWithDetailsAsync(id);
            return await MapToShowtimeDtoAsync(updatedShowtime ?? showtime);
        }

        public async Task<bool> DeleteShowtimeAsync(int id)
        {
            var showtime = await _showtimeRepository.GetByIdAsync(id);
            if (showtime == null) return false;

            _showtimeRepository.Delete(showtime);
            return await _showtimeRepository.SaveChangesAsync();
        }

        private async Task<ShowtimeDto> MapToShowtimeDtoAsync(Showtime showtime)
        {
            var dto = _mapper.Map<ShowtimeDto>(showtime);
            var bookedSeats = await _showtimeRepository.GetBookedSeatsCountAsync(showtime.ShowtimeId);
            dto.AvailableSeats = Math.Max(0, dto.TotalSeats - bookedSeats);
            return dto;
        }
    }
}
