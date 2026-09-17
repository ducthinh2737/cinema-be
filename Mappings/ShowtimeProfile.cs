using AutoMapper;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Mappings
{
    public class ShowtimeProfile : Profile
    {
        public ShowtimeProfile()
        {
            CreateMap<Showtime, ShowtimeDto>()
                .ForMember(dest => dest.MovieTitle, opt => opt.MapFrom(src => src.Movie != null ? src.Movie.Title : string.Empty))
                .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.Hall != null ? src.Hall.HallName : string.Empty))
                .ForMember(dest => dest.CinemaId, opt => opt.MapFrom(src => src.Hall != null ? src.Hall.CinemaId : 0))
                .ForMember(dest => dest.CinemaName, opt => opt.MapFrom(src => (src.Hall != null && src.Hall.Cinema != null) ? src.Hall.Cinema.CinemaName : string.Empty))
                .ForMember(dest => dest.PriceValue, opt => opt.MapFrom(src => src.Price != null ? src.Price.Value : 0m))
                .ForMember(dest => dest.TicketType, opt => opt.MapFrom(src => src.Price != null ? src.Price.TicketType : string.Empty))
                .ForMember(dest => dest.TotalSeats, opt => opt.MapFrom(src => (src.Hall != null && src.Hall.Seats != null) ? src.Hall.Seats.Count : 0))
                .ForMember(dest => dest.AvailableSeats, opt => opt.Ignore())
                .ForMember(dest => dest.Hall, opt => opt.MapFrom(src => src.Hall))
                .ForMember(dest => dest.Movie, opt => opt.MapFrom(src => src.Movie));

            CreateMap<ShowtimeCreateDto, Showtime>();
            CreateMap<ShowtimeUpdateDto, Showtime>();

            CreateMap<PricingRule, PricingRuleDto>();
            CreateMap<PricingRuleCreateUpdateDto, PricingRule>();
        }
    }
}
