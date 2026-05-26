using System.Threading.Tasks;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Logs;

namespace CinemaBooking.API.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByBookingIdAsync(int bookingId);
        Task AddAsync(Payment payment);
        void Update(Payment payment);

        Task AddTransactionAsync(PaymentTransaction transaction);
        Task<PaymentTransaction?> GetTransactionByRefAsync(string reference);
        void UpdateTransaction(PaymentTransaction transaction);

        Task AddRefundAsync(Refund refund);
        Task<Refund?> GetRefundByIdAsync(int refundId);

        Task AddAuditLogAsync(AuditLog auditLog);

        Task<bool> SaveChangesAsync();
    }
}
