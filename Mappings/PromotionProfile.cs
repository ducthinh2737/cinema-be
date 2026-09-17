using AutoMapper;
using CinemaBooking.API.Models.Promotions;
using CinemaBooking.API.DTOs.Promotions;

namespace CinemaBooking.API.Mappings
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, PromotionDto>()
                .ForMember(dest => dest.PromotionConditions, opt => opt.MapFrom(src => src.PromotionConditions));
                
            CreateMap<PromotionCreateDto, Promotion>()
                .ForMember(dest => dest.PromotionConditions, opt => opt.MapFrom(src => src.PromotionConditions));
                
            CreateMap<PromotionUpdateDto, Promotion>()
                .ForMember(dest => dest.PromotionConditions, opt => opt.MapFrom(src => src.PromotionConditions));

            CreateMap<PromotionCondition, PromotionConditionDto>();
            CreateMap<PromotionConditionCreateDto, PromotionCondition>();
        }
    }
}
