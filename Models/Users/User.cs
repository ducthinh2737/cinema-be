using CinemaBooking.API.Models.Bookings;
namespace CinemaBooking.API.Models.Users
{
    public class User
    {
        public int UserId { get; set; }

        public string Email { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? AvatarUrl { get; set; }

        public bool IsEmailVerified { get; set; }

        public bool IsPhoneVerified { get; set; }

        public bool IsActive { get; set; }

        public int MembershipPoints { get; set; }
        public int LifetimePoints { get; set; } = 0;
        public int? MemberTierId { get; set; }
        public MemberTier? MemberTier { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

        public string? EmailVerificationToken { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? ResetTokenExpires { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
