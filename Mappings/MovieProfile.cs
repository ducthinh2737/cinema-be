using AutoMapper;
using CinemaBooking.API.DTOs.Movies;
using CinemaBooking.API.Models.Movies;

namespace CinemaBooking.API.Mappings
{
    public class MovieProfile : Profile
    {
        public MovieProfile()
        {
            CreateMap<Movie, MovieDto>()
                .ForMember(dest => dest.GenreName, opt => opt.MapFrom(src => src.Genre != null ? src.Genre.GenreName : string.Empty))
                .ForMember(dest => dest.DirectorName, opt => opt.MapFrom(src => src.Director != null ? src.Director.FullName : string.Empty));

            CreateMap<Movie, MovieDetailDto>()
                .ForMember(dest => dest.GenreName, opt => opt.MapFrom(src => src.Genre != null ? src.Genre.GenreName : string.Empty))
                .ForMember(dest => dest.DirectorName, opt => opt.MapFrom(src => src.Director != null ? src.Director.FullName : string.Empty))
                .ForMember(dest => dest.Actors, opt => opt.MapFrom(src => src.MovieActors));

            CreateMap<MovieActor, MovieActorDto>()
                .ForMember(dest => dest.ActorId, opt => opt.MapFrom(src => src.ActorId))
                .ForMember(dest => dest.ActorName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.FullName : string.Empty));

            CreateMap<MovieCreateDto, Movie>()
                .ForMember(dest => dest.MovieActors, opt => opt.Ignore());

            CreateMap<MovieUpdateDto, Movie>()
                .ForMember(dest => dest.MovieActors, opt => opt.Ignore());
        }
    }
}
