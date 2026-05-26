using System.Threading.Tasks;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRefreshTokenAsync(string token);
        Task<User?> GetByResetTokenAsync(string token);
        Task<User?> GetByVerificationTokenAsync(string token);
        Task AddAsync(User user);
        void Update(User user);
        Task SaveChangesAsync();
    }
}
