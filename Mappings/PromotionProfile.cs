using AutoMapper;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.DTOs.Promotions;

namespace CinemaBooking.API.Mappings
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, PromotionDto>();
            CreateMap<PromotionCreateDto, Promotion>();
            CreateMap<PromotionUpdateDto, Promotion>();
        }
    }
}
