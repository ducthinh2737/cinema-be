using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CinemaBooking.API.DTOs.Cinemas;
using CinemaBooking.API.DTOs.Common;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Repositories.Interfaces;
using CinemaBooking.API.Services.Interfaces;

namespace CinemaBooking.API.Services.Implementations
{
    public class CinemaService : ICinemaService
    {
        private readonly ICinemaRepository _cinemaRepository;
        private readonly IMapper _mapper;

        public CinemaService(ICinemaRepository cinemaRepository, IMapper mapper)
        {
            _cinemaRepository = cinemaRepository;
            _mapper = mapper;
        }

        // Cinema
        public async Task<PagedResultDto<CinemaDto>> GetPagedCinemasAsync(CinemaQueryParameters queryParams)
        {
            var (cinemas, totalCount) = await _cinemaRepository.GetPagedCinemasAsync(queryParams);
            var dtos = _mapper.Map<List<CinemaDto>>(cinemas);

            return new PagedResultDto<CinemaDto>
            {
                Items = dtos,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<CinemaDto?> GetCinemaByIdAsync(int id)
        {
            var cinema = await _cinemaRepository.GetCinemaByIdAsync(id);
            return _mapper.Map<CinemaDto>(cinema);
        }

        public async Task<CinemaDto> CreateCinemaAsync(CinemaCreateDto createDto)
        {
            var cinema = _mapper.Map<Cinema>(createDto);
            await _cinemaRepository.AddCinemaAsync(cinema);
            await _cinemaRepository.SaveChangesAsync();

            // Load nested details
            var dbCinema = await _cinemaRepository.GetCinemaByIdAsync(cinema.CinemaId);
            return _mapper.Map<CinemaDto>(dbCinema);
        }

        public async Task<CinemaDto?> UpdateCinemaAsync(int id, CinemaUpdateDto updateDto)
        {
            var cinema = await _cinemaRepository.GetCinemaByIdAsync(id);
            if (cinema == null) return null;

            _mapper.Map(updateDto, cinema);
            await _cinemaRepository.UpdateCinemaAsync(cinema);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<CinemaDto>(cinema);
        }

        public async Task<bool> DeleteCinemaAsync(int id)
        {
            var cinema = await _cinemaRepository.GetCinemaByIdAsync(id);
            if (cinema == null) return false;

            await _cinemaRepository.DeleteCinemaAsync(cinema);
            return await _cinemaRepository.SaveChangesAsync();
        }

        public async Task<CinemaDto?> UpdateCinemaImageAsync(int id, string imageUrl)
        {
            var cinema = await _cinemaRepository.GetCinemaByIdAsync(id);
            if (cinema == null) return null;

            cinema.ImageUrl = imageUrl;
            await _cinemaRepository.UpdateCinemaAsync(cinema);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<CinemaDto>(cinema);
        }

        // Hall
        public async Task<IEnumerable<HallDto>> GetHallsByCinemaIdAsync(int cinemaId)
        {
            var halls = await _cinemaRepository.GetHallsByCinemaIdAsync(cinemaId);
            return _mapper.Map<IEnumerable<HallDto>>(halls);
        }

        public async Task<HallDto?> GetHallByIdAsync(int id)
        {
            var hall = await _cinemaRepository.GetHallByIdAsync(id);
            return _mapper.Map<HallDto>(hall);
        }

        public async Task<HallDto> CreateHallAsync(HallCreateDto createDto)
        {
            var hall = _mapper.Map<Hall>(createDto);
            await _cinemaRepository.AddHallAsync(hall);
            await _cinemaRepository.SaveChangesAsync();

            var dbHall = await _cinemaRepository.GetHallByIdAsync(hall.HallId);
            return _mapper.Map<HallDto>(dbHall);
        }

        public async Task<HallDto?> UpdateHallAsync(int id, HallUpdateDto updateDto)
        {
            var hall = await _cinemaRepository.GetHallByIdAsync(id);
            if (hall == null) return null;

            _mapper.Map(updateDto, hall);
            await _cinemaRepository.UpdateHallAsync(hall);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<HallDto>(hall);
        }

        public async Task<bool> DeleteHallAsync(int id)
        {
            var hall = await _cinemaRepository.GetHallByIdAsync(id);
            if (hall == null) return false;

            await _cinemaRepository.DeleteHallAsync(hall);
            return await _cinemaRepository.SaveChangesAsync();
        }

        // Seat
        public async Task<IEnumerable<SeatDto>> GetSeatsByHallIdAsync(int hallId)
        {
            var seats = await _cinemaRepository.GetSeatsByHallIdAsync(hallId);
            return _mapper.Map<IEnumerable<SeatDto>>(seats);
        }

        public async Task<SeatDto?> GetSeatByIdAsync(int id)
        {
            var seat = await _cinemaRepository.GetSeatByIdAsync(id);
            return _mapper.Map<SeatDto>(seat);
        }

        public async Task<SeatDto> CreateSeatAsync(SeatCreateDto createDto)
        {
            var seat = _mapper.Map<Seat>(createDto);
            await _cinemaRepository.AddSeatAsync(seat);
            await _cinemaRepository.SaveChangesAsync();

            var dbSeat = await _cinemaRepository.GetSeatByIdAsync(seat.SeatId);
            return _mapper.Map<SeatDto>(dbSeat);
        }

        public async Task<SeatDto?> UpdateSeatAsync(int id, SeatUpdateDto updateDto)
        {
            var seat = await _cinemaRepository.GetSeatByIdAsync(id);
            if (seat == null) return null;

            _mapper.Map(updateDto, seat);
            await _cinemaRepository.UpdateSeatAsync(seat);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<SeatDto>(seat);
        }

        public async Task<bool> DeleteSeatAsync(int id)
        {
            var seat = await _cinemaRepository.GetSeatByIdAsync(id);
            if (seat == null) return false;

            await _cinemaRepository.DeleteSeatAsync(seat);
            return await _cinemaRepository.SaveChangesAsync();
        }

        // HallType
        public async Task<IEnumerable<HallTypeDto>> GetHallTypesAsync()
        {
            var types = await _cinemaRepository.GetHallTypesAsync();
            return _mapper.Map<IEnumerable<HallTypeDto>>(types);
        }

        public async Task<HallTypeDto?> GetHallTypeByIdAsync(int id)
        {
            var type = await _cinemaRepository.GetHallTypeByIdAsync(id);
            return _mapper.Map<HallTypeDto>(type);
        }

        public async Task<HallTypeDto> CreateHallTypeAsync(HallTypeCreateDto createDto)
        {
            var type = _mapper.Map<HallType>(createDto);
            await _cinemaRepository.AddHallTypeAsync(type);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<HallTypeDto>(type);
        }

        public async Task<HallTypeDto?> UpdateHallTypeAsync(int id, HallTypeUpdateDto updateDto)
        {
            var type = await _cinemaRepository.GetHallTypeByIdAsync(id);
            if (type == null) return null;

            _mapper.Map(updateDto, type);
            await _cinemaRepository.UpdateHallTypeAsync(type);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<HallTypeDto>(type);
        }

        public async Task<bool> DeleteHallTypeAsync(int id)
        {
            var type = await _cinemaRepository.GetHallTypeByIdAsync(id);
            if (type == null) return false;

            await _cinemaRepository.DeleteHallTypeAsync(type);
            return await _cinemaRepository.SaveChangesAsync();
        }

        // SeatType
        public async Task<IEnumerable<SeatTypeDto>> GetSeatTypesAsync()
        {
            var types = await _cinemaRepository.GetSeatTypesAsync();
            return _mapper.Map<IEnumerable<SeatTypeDto>>(types);
        }

        public async Task<SeatTypeDto?> GetSeatTypeByIdAsync(int id)
        {
            var type = await _cinemaRepository.GetSeatTypeByIdAsync(id);
            return _mapper.Map<SeatTypeDto>(type);
        }

        public async Task<SeatTypeDto> CreateSeatTypeAsync(SeatTypeCreateDto createDto)
        {
            var type = _mapper.Map<SeatType>(createDto);
            await _cinemaRepository.AddSeatTypeAsync(type);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<SeatTypeDto>(type);
        }

        public async Task<SeatTypeDto?> UpdateSeatTypeAsync(int id, SeatTypeUpdateDto updateDto)
        {
            var type = await _cinemaRepository.GetSeatTypeByIdAsync(id);
            if (type == null) return null;

            _mapper.Map(updateDto, type);
            await _cinemaRepository.UpdateSeatTypeAsync(type);
            await _cinemaRepository.SaveChangesAsync();

            return _mapper.Map<SeatTypeDto>(type);
        }

        public async Task<bool> DeleteSeatTypeAsync(int id)
        {
            var type = await _cinemaRepository.GetSeatTypeByIdAsync(id);
            if (type == null) return false;

            await _cinemaRepository.DeleteSeatTypeAsync(type);
            return await _cinemaRepository.SaveChangesAsync();
        }
    }
}
