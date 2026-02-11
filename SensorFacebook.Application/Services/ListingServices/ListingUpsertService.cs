using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System.Text.Json;

namespace SensorFacebook.Application.Services.ListingServices
{
    public sealed class ListingUpsertService : IListingUpsertService
    {
        private readonly SensorDbContext _db;
        public ListingUpsertService(SensorDbContext db) { _db = db; }

        public async Task<int> UpsertAsync(
            SearchExecutor.SearchItem it,
            int? keywordId,
            Guid? accountId,
            int? proxyGroupId,          
            DateTimeOffset now,
            CancellationToken ct = default)
        {
            var nowUtc = now.UtcDateTime;

            var row = await _db.Listings.FirstOrDefaultAsync(x => x.ExternalId == it.ExternalId, ct);
            if (row is null)
            {
                row = new Listing
                {
                    Id = Guid.NewGuid(),
                    ExternalId = it.ExternalId,
                    KeywordId = keywordId,
                    AccountId = accountId,

                    

                    Title = it.Title,
                    Price = it.Price,
                    Currency = it.Currency,
                    Location = it.LocationText,
                    Condition = it.Condition,
                    IsActive = !(it.IsSold ?? false),
                    FirstSeen = nowUtc,
                    LastSeen = nowUtc,
                    Payload = it.PayloadJson
                };
                _db.Listings.Add(row);

                _db.ListingChanges.Add(new ListingChange
                {
                    Id = Guid.NewGuid(),
                    ListingId = row.Id,
                    ChangeType = "created",
                    OldValue = null,
                    NewValue = it.PayloadJson ?? "{}",     
                    OccurredAt = nowUtc                    
                });

                await _db.SaveChangesAsync(ct);
                return 1;
            }
            else
            {
                var changed = false;

                if (row.Title != it.Title)
                {
                    _db.ListingChanges.Add(new ListingChange
                    {
                        Id = Guid.NewGuid(),
                        ListingId = row.Id,
                        ChangeType = "title",
                        OldValue = JsonSerializer.Serialize(row.Title), 
                        NewValue = JsonSerializer.Serialize(it.Title), 
                        OccurredAt = nowUtc
                    });
                    row.Title = it.Title;
                    changed = true;
                }

                if (row.Price != it.Price)
                {
                    _db.ListingChanges.Add(new ListingChange
                    {
                        Id = Guid.NewGuid(),
                        ListingId = row.Id,
                        ChangeType = "price",
                        OldValue = JsonSerializer.Serialize(row.Price),
                        NewValue = JsonSerializer.Serialize(it.Price),
                        OccurredAt = nowUtc
                    });
                    row.Price = it.Price;
                    changed = true;
                }

                var newActive = !(it.IsSold ?? false);
                if (row.IsActive != newActive)
                {
                    _db.ListingChanges.Add(new ListingChange
                    {
                        Id = Guid.NewGuid(),
                        ListingId = row.Id,
                        ChangeType = "active",
                        OldValue = JsonSerializer.Serialize(row.IsActive),
                        NewValue = JsonSerializer.Serialize(newActive),
                        OccurredAt = nowUtc
                    });
                    row.IsActive = newActive;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(it.PayloadJson))
                {
                    // nếu muốn log payload change, thêm ListingChange type="payload"
                    row.Payload = it.PayloadJson;
                    changed = true;
                }

                row.LastSeen = nowUtc;

                if (changed)
                    await _db.SaveChangesAsync(ct);

                return changed ? 1 : 0;
            }
        }
    }
}
