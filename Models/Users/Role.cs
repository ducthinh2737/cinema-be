using System.Collections.Generic;

namespace CinemaBooking.API.Models.Users
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
