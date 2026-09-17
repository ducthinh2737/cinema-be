using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CinemaBooking.API.Data;
using CinemaBooking.API.Models.Users;

namespace CinemaBooking.API.BackgroundServices
{
    public class LoyaltyPointsReleaseWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoyaltyPointsReleaseWorker> _logger;

        public LoyaltyPointsReleaseWorker(IServiceProvider serviceProvider, ILogger<LoyaltyPointsReleaseWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LoyaltyPointsReleaseWorker started running.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

                    var now = DateTime.UtcNow;

                    // Query pending loyalty points transactions where the associated showtime has ended
                    var pendingTransactions = await context.LoyaltyTransactions
                        .Include(t => t.User)
                        .Include(t => t.Booking).ThenInclude(b => b!.Showtime)
                        .Where(t => t.Status == "Pending" 
                                 && t.TransactionType == "Earn" 
                                 && t.Booking != null 
                                 && t.Booking.Showtime.StartTime.AddMinutes(t.Booking.Showtime.Movie.Duration) < now)
                        .ToListAsync(stoppingToken);

                    if (pendingTransactions.Any())
                    {
                        _logger.LogInformation("Processing {Count} pending loyalty point completions.", pendingTransactions.Count);
                        
                        foreach (var transaction in pendingTransactions)
                        {
                            transaction.Status = "Completed";
                            
                            var user = transaction.User;
                            user.MembershipPoints += transaction.PointsChanged;
                            user.LifetimePoints += transaction.PointsChanged;

                            // Re-evaluate member rank tier
                            await EvaluateUserTierAsync(context, user, stoppingToken);
                        }

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Successfully completed {Count} loyalty point reward actions.", pendingTransactions.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in LoyaltyPointsReleaseWorker execution cycle.");
                }

                // Check and release every 5 minutes (for quicker testing / live updates)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task EvaluateUserTierAsync(CinemaDbContext context, User user, CancellationToken cancellationToken)
        {
            var tiers = await context.MemberTiers
                .OrderByDescending(t => t.MinPoints)
                .ToListAsync(cancellationToken);

            foreach (var tier in tiers)
            {
                if (user.LifetimePoints >= tier.MinPoints)
                {
                    if (user.MemberTierId != tier.MemberTierId)
                    {
                        _logger.LogInformation("User {UserId} upgraded to Member Rank: {TierName}", user.UserId, tier.TierName);
                        user.MemberTierId = tier.MemberTierId;
                    }
                    break;
                }
            }
        }
    }
}
