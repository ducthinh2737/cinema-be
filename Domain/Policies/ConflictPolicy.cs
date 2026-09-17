using System;

namespace CinemaBooking.API.Domain.Policies
{
    public class ConflictPolicy
    {
        public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB, int bufferMinutes = 15)
        {
            var endAWithBuffer = endA.AddMinutes(bufferMinutes);
            var endBWithBuffer = endB.AddMinutes(bufferMinutes);

            return startA < endBWithBuffer && startB < endAWithBuffer;
        }

        public static string GetConflictReason(string movieTitle, DateTime startLocal, DateTime endLocal)
        {
            return $"Xung đột với suất phim \"{movieTitle}\" ({startLocal:HH:mm} - {endLocal:HH:mm}).";
        }
    }
}
