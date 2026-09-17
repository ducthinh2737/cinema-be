using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;

namespace CinemaBooking.API.Services.Implementations.BatchShowtimes
{
    public class ConflictDetectionService
    {
        private readonly CinemaDbContext _context;

        public ConflictDetectionService(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<(HallTimeSlotPlanner planner, int queryCount)> LoadHallPlannerAsync(
            int hallId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken)
        {
            int queryCount = 0;
            var planner = new HallTimeSlotPlanner(hallId);

            var startThreshold = startDate.Date.AddDays(-1);
            var endThreshold = endDate.Date.AddDays(2);

            var existingShowtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .AsNoTracking()
                .Where(s => s.HallId == hallId && s.StartTime >= startThreshold && s.StartTime < endThreshold)
                .ToListAsync(cancellationToken);
            
            queryCount++;

            planner.AddExistingShowtimes(existingShowtimes);
            return (planner, queryCount);
        }
    }
}
