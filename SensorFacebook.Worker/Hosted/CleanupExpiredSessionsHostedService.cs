using SensorFacebook.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Worker.Hosted
{
    public sealed class CleanupExpiredSessionsHostedService : BackgroundService
    {
        private readonly ILogger<CleanupExpiredSessionsHostedService> _log;
        private readonly IServiceProvider _sp;

        public CleanupExpiredSessionsHostedService(ILogger<CleanupExpiredSessionsHostedService> log, IServiceProvider sp)
        { _log = log; _sp = sp; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delay = TimeSpan.FromMinutes(2);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
                    var now = DateTimeOffset.UtcNow.UtcDateTime;

                    var expired = await db.Sessions
                        .Where(s => s.EndedAt == null && s.ExpiresAt < now)
                        .ToListAsync(stoppingToken);

                    if (expired.Count > 0)
                    {
                        foreach (var s in expired) s.EndedAt = now;
                        await db.SaveChangesAsync(stoppingToken);
                        _log.LogInformation("Cleaned {Count} expired sessions", expired.Count);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "CleanupExpiredSessions failed");
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
