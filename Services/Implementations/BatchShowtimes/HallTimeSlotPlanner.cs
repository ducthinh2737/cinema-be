using System;
using System.Collections.Generic;
using CinemaBooking.API.Models.Showtimes;

namespace CinemaBooking.API.Services.Implementations.BatchShowtimes
{
    public class TimeInterval
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; } // End time inclusive of 15 min cleaning buffer
        public int? ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = null!;
    }

    public class HallTimeSlotPlanner
    {
        private readonly List<TimeInterval> _intervals = new();

        public int HallId { get; }

        public HallTimeSlotPlanner(int hallId)
        {
            HallId = hallId;
        }

        public void AddExistingShowtimes(IEnumerable<Showtime> showtimes)
        {
            foreach (var st in showtimes)
            {
                _intervals.Add(new TimeInterval
                {
                    Start = st.StartTime,
                    End = st.EndTime.AddMinutes(15),
                    ShowtimeId = st.ShowtimeId,
                    MovieTitle = st.Movie?.Title ?? "Phim đã xếp lịch"
                });
            }
            SortIntervals();
        }

        public void AddPlannedShowtime(DateTime startTime, DateTime endTime, string movieTitle)
        {
            _intervals.Add(new TimeInterval
            {
                Start = startTime,
                End = endTime.AddMinutes(15),
                MovieTitle = movieTitle
            });
            SortIntervals();
        }

        public (bool hasConflict, TimeInterval? conflictingInterval) CheckConflict(DateTime start, DateTime end)
        {
            var endWithBuffer = end.AddMinutes(15);
            
            // Linear search over pre-sorted and small interval list is O(N) and very fast in practice.
            foreach (var interval in _intervals)
            {
                if (start < interval.End && interval.Start < endWithBuffer)
                {
                    return (true, interval);
                }
            }
            return (false, null);
        }

        private void SortIntervals()
        {
            _intervals.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        public List<TimeInterval> GetIntervals() => _intervals;
    }
}
