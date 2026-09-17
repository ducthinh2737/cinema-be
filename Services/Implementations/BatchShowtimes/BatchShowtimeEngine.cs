using System;
using System.Collections.Generic;
using System.Linq;
using CinemaBooking.API.DTOs.Showtimes;
using CinemaBooking.API.Models.Movies;
using CinemaBooking.API.Models.Cinemas;
using CinemaBooking.API.Domain.Policies;

namespace CinemaBooking.API.Services.Implementations.BatchShowtimes
{
    public class ShowtimeCandidate
    {
        public DateTime StartTime { get; set; } // UTC
        public DateTime EndTime { get; set; }   // UTC
        public bool IsValid { get; set; }
        public string? ConflictReason { get; set; }
    }

    public class BatchShowtimeEngine
    {
        private static readonly TimeZoneInfo LocalTimeZone = ShowtimeSchedulingPolicy.LocalTimeZone;

        public (List<ShowtimeCandidate> candidates, List<SkippedSlotDto> skipped, List<ConflictDetailDto> conflicts) GenerateShowtimes(
            ShowtimeBatchRequestDto batchDto,
            Movie movie,
            Cinema cinema,
            HallTimeSlotPlanner planner)
        {
            var candidates = new List<ShowtimeCandidate>();
            var skipped = new List<SkippedSlotDto>();
            var conflicts = new List<ConflictDetailDto>();

            var localStartDate = TimeZoneInfo.ConvertTimeFromUtc(batchDto.StartDate.ToUniversalTime(), LocalTimeZone).Date;
            var localEndDate = TimeZoneInfo.ConvertTimeFromUtc(batchDto.EndDate.ToUniversalTime(), LocalTimeZone).Date;

            // Mode can be: "Manual" or "Auto"
            // Let's determine the engine scheduling type (Fixed, Dynamic, Hybrid)
            // If IntervalType is "Daily" or "Custom" -> we use Fixed Interval logic
            // If IntervalType is "Hourly" -> we can use Hybrid / Dynamic spacing
            // Let's implement the three modes:
            
            for (var currentDate = localStartDate; currentDate <= localEndDate; currentDate = currentDate.AddDays(1))
            {
                if (batchDto.Mode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    // Dynamic Slot scheduling or Hybrid
                    if (batchDto.IntervalType.Equals("Hourly", StringComparison.OrdinalIgnoreCase))
                    {
                        GenerateHybridForDay(currentDate, batchDto, movie, cinema, planner, candidates, skipped, conflicts);
                    }
                    else
                    {
                        GenerateDynamicForDay(currentDate, batchDto, movie, cinema, planner, candidates, skipped, conflicts);
                    }
                }
                else
                {
                    // Fixed Interval Mode
                    GenerateFixedForDay(currentDate, batchDto, movie, cinema, planner, candidates, skipped, conflicts);
                }
            }

            return (candidates, skipped, conflicts);
        }

        /// <summary>
        /// FIXED INTERVAL mode: Uses custom / fixed start times.
        /// If a conflict is found in Manual mode, returns conflicts.
        /// </summary>
        private void GenerateFixedForDay(
            DateTime localDate,
            ShowtimeBatchRequestDto batchDto,
            Movie movie,
            Cinema cinema,
            HallTimeSlotPlanner planner,
            List<ShowtimeCandidate> candidates,
            List<SkippedSlotDto> skipped,
            List<ConflictDetailDto> conflicts)
        {
            var targetTimes = GetTargetTimesForDay(batchDto);

            foreach (var time in targetTimes)
            {
                var candidateStartLocal = localDate.Date.Add(time);
                var candidateEndLocal = ShowtimeSchedulingPolicy.CalculateEndTime(movie, candidateStartLocal);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateStartLocal, DateTimeKind.Unspecified), LocalTimeZone);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateEndLocal, DateTimeKind.Unspecified), LocalTimeZone);

                try
                {
                    // 1. Release window check
                    ShowtimeSchedulingPolicy.ValidateReleaseWindow(movie, startUtc);

                    // 2. Working hours check
                    ShowtimeSchedulingPolicy.ValidateOperatingHours(startUtc, endUtc, cinema);

                    // 3. Past check
                    if (startUtc <= DateTime.UtcNow)
                    {
                        conflicts.Add(new ConflictDetailDto
                        {
                            StartTime = startUtc,
                            EndTime = endUtc,
                            Reason = "Thời gian bắt đầu của suất chiếu phải ở trong tương lai."
                        });
                        continue;
                    }

                    // 4. Overlap check
                    var (hasConflict, conflictingInterval) = planner.CheckConflict(startUtc, endUtc);
                    if (!hasConflict)
                    {
                        planner.AddPlannedShowtime(startUtc, endUtc, movie.Title);
                        candidates.Add(new ShowtimeCandidate { StartTime = startUtc, EndTime = endUtc, IsValid = true });
                    }
                    else
                    {
                        var confStartLocal = TimeZoneInfo.ConvertTimeFromUtc(conflictingInterval!.Start, LocalTimeZone);
                        var confEndLocal = TimeZoneInfo.ConvertTimeFromUtc(conflictingInterval.End.AddMinutes(-ShowtimeSchedulingPolicy.CleanUpBufferMinutes), LocalTimeZone);

                        conflicts.Add(new ConflictDetailDto
                        {
                            StartTime = startUtc,
                            EndTime = endUtc,
                            Reason = ConflictPolicy.GetConflictReason(conflictingInterval.MovieTitle, confStartLocal, confEndLocal),
                            ConflictingShowtimeInfo = $"{conflictingInterval.MovieTitle} ({confStartLocal:dd/MM/yyyy HH:mm} - {confEndLocal:HH:mm})"
                        });
                    }
                }
                catch (Exception ex)
                {
                    conflicts.Add(new ConflictDetailDto
                    {
                        StartTime = startUtc,
                        EndTime = endUtc,
                        Reason = ex.Message
                    });
                }
            }
        }

        /// <summary>
        /// DYNAMIC SLOT mode: Arranges slots back-to-back starting at opening hours,
        /// automatically skipping conflicting blocks.
        /// </summary>
        private void GenerateDynamicForDay(
            DateTime localDate,
            ShowtimeBatchRequestDto batchDto,
            Movie movie,
            Cinema cinema,
            HallTimeSlotPlanner planner,
            List<ShowtimeCandidate> candidates,
            List<SkippedSlotDto> skipped,
            List<ConflictDetailDto> conflicts)
        {
            TimeSpan.TryParse(cinema.OpeningTime ?? "08:00", out var openTs);

            var dayStartLocal = localDate.Date.Add(openTs);
            var dayEndLocal = localDate.Date.AddHours(23).AddMinutes(59);
            var currentCandidateStartLocal = dayStartLocal;
            var showsCreated = 0;

            while (showsCreated < batchDto.MaxShowsPerDay && currentCandidateStartLocal <= dayEndLocal)
            {
                var candidateEndLocal = ShowtimeSchedulingPolicy.CalculateEndTime(movie, currentCandidateStartLocal);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(currentCandidateStartLocal, DateTimeKind.Unspecified), LocalTimeZone);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateEndLocal, DateTimeKind.Unspecified), LocalTimeZone);

                if (startUtc <= DateTime.UtcNow)
                {
                    // Jump to future
                    var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LocalTimeZone).AddMinutes(5);
                    var minute = nowLocal.Minute;
                    var remainder = minute % 5;
                    currentCandidateStartLocal = nowLocal.AddMinutes(5 - remainder).AddSeconds(-nowLocal.Second).AddMilliseconds(-nowLocal.Millisecond);
                    continue;
                }

                // Verify working hours limit: must start before or at closing time
                if (currentCandidateStartLocal > dayEndLocal)
                {
                    break;
                }

                var (hasConflict, conflictingInterval) = planner.CheckConflict(startUtc, endUtc);
                if (!hasConflict)
                {
                    planner.AddPlannedShowtime(startUtc, endUtc, movie.Title);
                    candidates.Add(new ShowtimeCandidate { StartTime = startUtc, EndTime = endUtc, IsValid = true });
                    showsCreated++;
                    currentCandidateStartLocal = candidateEndLocal.AddMinutes(ShowtimeSchedulingPolicy.CleanUpBufferMinutes);
                }
                else
                {
                    var conflictEndLocal = TimeZoneInfo.ConvertTimeFromUtc(conflictingInterval!.End, LocalTimeZone);
                    skipped.Add(new SkippedSlotDto
                    {
                        StartTime = startUtc,
                        Reason = $"Xung đột với suất phim \"{conflictingInterval.MovieTitle}\" (UTC {conflictingInterval.Start:HH:mm} - {conflictingInterval.End.AddMinutes(-ShowtimeSchedulingPolicy.CleanUpBufferMinutes):HH:mm}). Tự động nhảy sang slot tiếp theo lúc {conflictEndLocal:HH:mm}."
                    });
                    currentCandidateStartLocal = conflictEndLocal;
                }
            }
        }

        /// <summary>
        /// HYBRID mode: Starts from target guidelines, but shifts downstream slots
        /// dynamically to guarantee safety buffers when durations run long.
        /// </summary>
        private void GenerateHybridForDay(
            DateTime localDate,
            ShowtimeBatchRequestDto batchDto,
            Movie movie,
            Cinema cinema,
            HallTimeSlotPlanner planner,
            List<ShowtimeCandidate> candidates,
            List<SkippedSlotDto> skipped,
            List<ConflictDetailDto> conflicts)
        {
            var targetTimes = GetTargetTimesForDay(batchDto);
            var lastPlannedEndLocal = DateTime.MinValue;

            foreach (var time in targetTimes)
            {
                var candidateStartLocal = localDate.Date.Add(time);

                // If prior slot ran late, shift start to guarantee cleaning buffer
                if (lastPlannedEndLocal != DateTime.MinValue)
                {
                    var earliestPossibleStartLocal = lastPlannedEndLocal.AddMinutes(ShowtimeSchedulingPolicy.CleanUpBufferMinutes);
                    if (candidateStartLocal < earliestPossibleStartLocal)
                    {
                        var originalStart = candidateStartLocal;
                        candidateStartLocal = earliestPossibleStartLocal;
                        _ = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(originalStart, DateTimeKind.Unspecified), LocalTimeZone);
                        
                        skipped.Add(new SkippedSlotDto
                        {
                            StartTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(originalStart, DateTimeKind.Unspecified), LocalTimeZone),
                            Reason = $"Dãn khoảng cách để đảm bảo buffer dọn dẹp. Tự động dịch sang {candidateStartLocal:HH:mm}."
                        });
                    }
                }

                var candidateEndLocal = ShowtimeSchedulingPolicy.CalculateEndTime(movie, candidateStartLocal);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateStartLocal, DateTimeKind.Unspecified), LocalTimeZone);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateEndLocal, DateTimeKind.Unspecified), LocalTimeZone);

                if (startUtc <= DateTime.UtcNow)
                {
                    continue; // Skip past slots silently in auto mode
                }

                var (hasConflict, conflictingInterval) = planner.CheckConflict(startUtc, endUtc);
                if (!hasConflict)
                {
                    planner.AddPlannedShowtime(startUtc, endUtc, movie.Title);
                    candidates.Add(new ShowtimeCandidate { StartTime = startUtc, EndTime = endUtc, IsValid = true });
                    lastPlannedEndLocal = candidateEndLocal;
                }
                else
                {
                    var conflictEndLocal = TimeZoneInfo.ConvertTimeFromUtc(conflictingInterval!.End, LocalTimeZone);
                    skipped.Add(new SkippedSlotDto
                    {
                        StartTime = startUtc,
                        Reason = $"Xung đột với suất phim \"{conflictingInterval.MovieTitle}\" tại sảnh. Dịch chuyển sau mốc kết thúc ({conflictEndLocal:HH:mm})."
                    });
                    // Try planning again immediately after conflict
                    var retryStartLocal = conflictEndLocal;
                    var retryEndLocal = ShowtimeSchedulingPolicy.CalculateEndTime(movie, retryStartLocal);
                    var retryStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(retryStartLocal, DateTimeKind.Unspecified), LocalTimeZone);
                    var retryEndUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(retryEndLocal, DateTimeKind.Unspecified), LocalTimeZone);

                    var (retryConflict, _) = planner.CheckConflict(retryStartUtc, retryEndUtc);
                    if (!retryConflict)
                    {
                        planner.AddPlannedShowtime(retryStartUtc, retryEndUtc, movie.Title);
                        candidates.Add(new ShowtimeCandidate { StartTime = retryStartUtc, EndTime = retryEndUtc, IsValid = true });
                        lastPlannedEndLocal = retryEndLocal;
                    }
                }
            }
        }

        private List<TimeSpan> GetTargetTimesForDay(ShowtimeBatchRequestDto batchDto)
        {
            var targetTimes = new List<TimeSpan>();
            if (batchDto.IntervalType.Equals("Custom", StringComparison.OrdinalIgnoreCase) && batchDto.CustomTimes != null)
            {
                foreach (var tStr in batchDto.CustomTimes)
                {
                    if (TimeSpan.TryParse(tStr, out var ts))
                    {
                        targetTimes.Add(ts);
                    }
                }
            }
            else if (batchDto.IntervalType.Equals("Hourly", StringComparison.OrdinalIgnoreCase))
            {
                var startHour = 8;
                for (int i = 0; i < batchDto.MaxShowsPerDay; i++)
                {
                    var hour = startHour + (i * 3); // 3-hour chunks
                    if (hour <= 23)
                    {
                        targetTimes.Add(new TimeSpan(hour, 0, 0));
                    }
                }
            }
            else // "Daily"
            {
                var time = new TimeSpan(9, 0, 0);
                if (batchDto.CustomTimes != null && batchDto.CustomTimes.Count > 0 && TimeSpan.TryParse(batchDto.CustomTimes[0], out var ts))
                {
                    time = ts;
                }
                targetTimes.Add(time);
            }
            return targetTimes;
        }
    }
}
