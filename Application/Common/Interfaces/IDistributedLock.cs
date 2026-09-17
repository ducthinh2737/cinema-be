using System;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBooking.API.Application.Common.Interfaces
{
    public interface IDistributedLock
    {
        Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
