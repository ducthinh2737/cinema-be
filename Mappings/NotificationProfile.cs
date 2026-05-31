using AutoMapper;
using CinemaBooking.API.Models.Notifications;
using CinemaBooking.API.DTOs.Notifications;

namespace CinemaBooking.API.Mappings
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationDto>();
            CreateMap<Notification, NotificationSummaryDto>();
            CreateMap<Notification, NotificationDetailDto>();
            CreateMap<Notification, NotificationRealtimeDto>()
                .ForMember(dest => dest.UnreadCount, opt => opt.Ignore());
        }
    }
}
