using System.Threading;
using System.Threading.Tasks;

namespace CinemaBooking.API.Application.Common.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync<T>(string eventName, T payload, CancellationToken cancellationToken = default);
    }
}
