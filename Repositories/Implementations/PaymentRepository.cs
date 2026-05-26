using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Payments;
using CinemaBooking.API.Models.Logs;
using CinemaBooking.API.Repositories.Interfaces;

namespace CinemaBooking.API.Repositories.Implementations
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly CinemaDbContext _context;

        public PaymentRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments.FindAsync(id);
        }

        public async Task<Payment?> GetByBookingIdAsync(int bookingId)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public void Update(Payment payment)
        {
            _context.Payments.Update(payment);
        }

        public async Task AddTransactionAsync(PaymentTransaction transaction)
        {
            await _context.PaymentTransactions.AddAsync(transaction);
        }

        public async Task<PaymentTransaction?> GetTransactionByRefAsync(string reference)
        {
            return await _context.PaymentTransactions
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(t => t.TransactionReference == reference);
        }

        public void UpdateTransaction(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Update(transaction);
        }

        public async Task AddRefundAsync(Refund refund)
        {
            await _context.Refunds.AddAsync(refund);
        }

        public async Task<Refund?> GetRefundByIdAsync(int refundId)
        {
            return await _context.Refunds.FindAsync(refundId);
        }

        public async Task AddAuditLogAsync(AuditLog auditLog)
        {
            await _context.AuditLogs.AddAsync(auditLog);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
