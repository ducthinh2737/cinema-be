using AutoMapper;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.DTOs.Reviews;

namespace CinemaBooking.API.Mappings
{
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            CreateMap<Review, ReviewDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.MovieTitle, opt => opt.MapFrom(src => src.Movie.Title));

            CreateMap<ReviewCreateDto, Review>();
            CreateMap<ReviewUpdateDto, Review>();
        }
    }
}
