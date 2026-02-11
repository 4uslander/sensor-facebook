using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Messaging;
using SensorFacebook.Worker.Messaging;

namespace SensorFacebook.Worker.Handlers;

public sealed class ListingEnrichHandler : IMessageHandler<ListingEnrichMsg>
{
    private readonly IBrowserPool _pool;
    private readonly IAccountSelector _selector;
    private readonly SensorDbContext _db;

    public ListingEnrichHandler(IBrowserPool pool, IAccountSelector selector, SensorDbContext db)
    { _pool = pool; _selector = selector; _db = db; }

    public async Task HandleAsync(ListingEnrichMsg msg, CancellationToken ct)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == msg.ListingId, ct);
        if (listing is null) return;

        var url = msg.UrlOverride;
        if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(listing.ExternalId))
            url = $"https://www.facebook.com/marketplace/item/{listing.ExternalId}";
        if (string.IsNullOrWhiteSpace(url)) return;

        // chọn acc (nếu message không chỉ định, hãy Acquire theo proxy group của job/keyword)
        AccountLease? lease = null;
        if (msg.AccountId.HasValue)
        {
            if (!msg.ProxyGroupId.HasValue)
                throw new InvalidOperationException("ProxyGroupId is required when AccountId is specified.");

            lease = await _selector.AcquireByAccountAsync(
                msg.AccountId.Value,
                msg.ProxyGroupId.Value,
                "listing-enrich",
                TimeSpan.FromMinutes(10),
                ct);
        }
        else if (msg.ProxyGroupId.HasValue)
        {
            // You must provide LeasePriority and consumerKey as required by the signature
            lease = await _selector.AcquireAsync(
                msg.ProxyGroupId.Value,
                LeasePriority.Normal, // or another appropriate value
                consumerKey: "listing-enrich",
                ttl: TimeSpan.FromMinutes(10),
                ct: ct);
        }
        if (lease is null) return;

        try
        {
            await using var brLease = await _pool.AcquireAsync(lease.AccountId, lease.ProxyGroupId, ct);
            var page = (IPage)await brLease.NewPageAsync(ct);

            await page.GotoAsync(url, new() { Timeout = 30000, WaitUntil = WaitUntilState.DOMContentLoaded });

            // TODO: trích xuất, cập nhật listing.Payload, IsActive...
            await _db.SaveChangesAsync(ct);
        }
        finally
        {
            await _selector.ReleaseAsync(lease.SessionId, checkpoint: false, note: "enrich-done", ct);
        }
    }
}
