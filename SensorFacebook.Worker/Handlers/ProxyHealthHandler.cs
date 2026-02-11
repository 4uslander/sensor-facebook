using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Browser;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Messaging;
using SensorFacebook.Worker.Messaging;
using System.Diagnostics;

namespace SensorFacebook.Worker.Handlers;

public sealed class ProxyHealthHandler : IMessageHandler<ProxyHealthMsg>
{
    private readonly IBrowserPool _pool;
    private readonly SensorDbContext _db;

    public ProxyHealthHandler(IBrowserPool pool, SensorDbContext db)
    { _pool = pool; _db = db; }

    public async Task HandleAsync(ProxyHealthMsg msg, CancellationToken ct)
    {
        var t0 = Stopwatch.GetTimestamp();
        try
        {
            await using var lease = await _pool.AcquireAsync(null, msg.ProxyGroupId, ct);
            var page = (IPage)await lease.NewPageAsync(ct);
            await page.GotoAsync("https://m.facebook.com/marketplace", new() { Timeout = 15000 });
            var title = await page.TitleAsync();
            var ms = (int)Stopwatch.GetElapsedTime(t0).TotalMilliseconds;

            _db.ProxyHealths.Add(new ProxyHealth
            {
                ProxyGroupId = msg.ProxyGroupId,
                Healthy = true,
                LatencyMs = ms,
                LastStatus = title,
                CheckedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _db.ProxyHealths.Add(new ProxyHealth
            {
                ProxyGroupId = msg.ProxyGroupId,
                Healthy = false,
                LatencyMs = null,
                LastStatus = ex.GetType().Name,
                CheckedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
