using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Models.Users
{
    public class MemberTier
    {
        public int MemberTierId { get; set; }
        public string TierName { get; set; } = null!;      // Bronze, Silver, Gold, Platinum
        public int MinPoints { get; set; }                 // Minimum lifetime points required
        public decimal PointMultiplier { get; set; }       // Point earning multiplier (e.g. 1.0, 1.2, 1.5, 2.0)
        public string? BenefitsDescription { get; set; }   // JSON or text describing benefits

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
