using System.Linq;
using AutoMapper;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.DTOs.Cinemas;

namespace CinemaBooking.API.Mappings
{
    public class CinemaProfile : Profile
    {
        public CinemaProfile()
        {
            // Cinema
            CreateMap<Cinema, CinemaDto>()
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.City != null ? src.City.CityName : null))
                .ForMember(dest => dest.HallCount, opt => opt.MapFrom(src => src.Halls != null ? src.Halls.Count(h => !h.IsDeleted) : 0))
                .ForMember(dest => dest.SeatCount, opt => opt.MapFrom(src => src.Halls != null ? src.Halls.Where(h => !h.IsDeleted).Sum(h => h.Capacity) : 0));
            CreateMap<CinemaCreateDto, Cinema>();
            CreateMap<CinemaUpdateDto, Cinema>();

            // Hall
            CreateMap<Hall, HallDto>()
                .ForMember(dest => dest.CinemaName, opt => opt.MapFrom(src => src.Cinema != null ? src.Cinema.CinemaName : null))
                .ForMember(dest => dest.HallTypeName, opt => opt.MapFrom(src => src.HallType != null ? src.HallType.TypeName : null));
            CreateMap<HallCreateDto, Hall>();
            CreateMap<HallUpdateDto, Hall>();

            // Seat
            CreateMap<Seat, SeatDto>()
                .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.Hall != null ? src.Hall.HallName : null))
                .ForMember(dest => dest.SeatTypeName, opt => opt.MapFrom(src => src.SeatType != null ? src.SeatType.TypeName : null))
                .ForMember(dest => dest.RowName, opt => opt.MapFrom(src => src.SeatCode.Length > 0 ? src.SeatCode.Substring(0, 1) : ""))
                .ForMember(dest => dest.SeatNumber, opt => opt.MapFrom(src => ParseSeatNumber(src.SeatCode)));
            CreateMap<SeatCreateDto, Seat>();
            CreateMap<SeatUpdateDto, Seat>();

            // HallType
            CreateMap<HallType, HallTypeDto>();
            CreateMap<HallTypeCreateDto, HallType>();
            CreateMap<HallTypeUpdateDto, HallType>();

            // SeatType
            CreateMap<SeatType, SeatTypeDto>();
            CreateMap<SeatTypeCreateDto, SeatType>();
            CreateMap<SeatTypeUpdateDto, SeatType>();
        }

        private static int ParseSeatNumber(string seatCode)
        {
            if (string.IsNullOrEmpty(seatCode)) return 0;
            var cleanCode = seatCode.Split(':')[0];
            if (cleanCode.Length <= 1) return 0;
            return int.TryParse(cleanCode.Substring(1), out var num) ? num : 0;
        }
    }
}
