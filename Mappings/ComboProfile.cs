using System.Linq;
using AutoMapper;
using CinemaBooking.API.DTOs.Combos;
using CinemaBooking.API.Models.Combos;

namespace CinemaBooking.API.Mappings
{
    public class ComboProfile : Profile
    {
        public ComboProfile()
        {
            CreateMap<Product, ProductDto>();
            CreateMap<ProductCreateUpdateDto, Product>();

            CreateMap<ComboItem, ComboItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));

            CreateMap<ComboItemInputDto, ComboItem>();

            CreateMap<Combo, ComboDto>()
                .ForMember(dest => dest.ComboItems, opt => opt.MapFrom(src => src.ComboItems));

            CreateMap<ComboCreateUpdateDto, Combo>()
                .ForMember(dest => dest.ComboItems, opt => opt.Ignore()); // Will map manually in services due to quantity & existing products mapping

            CreateMap<OrderCombo, OrderComboDto>()
                .ForMember(dest => dest.ComboName, opt => opt.MapFrom(src => src.Combo != null ? src.Combo.Name : string.Empty));
        }
    }
}
