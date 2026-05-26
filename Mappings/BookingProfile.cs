using System.Linq;
using AutoMapper;
using CinemaBooking.API.DTOs.Bookings;
using CinemaBooking.API.Models.Bookings;

namespace CinemaBooking.API.Mappings
{
    public class BookingProfile : Profile
    {
        public BookingProfile()
        {
            CreateMap<Booking, BookingDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.MovieTitle, opt => opt.MapFrom(src => (src.Showtime != null && src.Showtime.Movie != null) ? src.Showtime.Movie.Title : string.Empty))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.Showtime != null ? src.Showtime.StartTime : System.DateTime.MinValue))
                .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => (src.Showtime != null && src.Showtime.Hall != null) ? src.Showtime.Hall.HallName : string.Empty))
                .ForMember(dest => dest.CinemaName, opt => opt.MapFrom(src => (src.Showtime != null && src.Showtime.Hall != null && src.Showtime.Hall.Cinema != null) ? src.Showtime.Hall.Cinema.CinemaName : string.Empty))
                .ForMember(dest => dest.Seats, opt => opt.MapFrom(src => src.BookingSeats != null ? src.BookingSeats.Select(bs => bs.Seat != null ? bs.Seat.SeatCode : string.Empty).ToList() : new System.Collections.Generic.List<string>()));
        }
    }
}
