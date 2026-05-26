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
        }
    }
}
