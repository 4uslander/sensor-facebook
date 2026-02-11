using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ListingServices
{
    public sealed class ListingQueryService : IListingQueryService
    {
        private readonly SensorDbContext _db;
        public ListingQueryService(SensorDbContext db) { _db = db; }

        private static string? BuildLink(string? externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId)) return null;

            // nếu DB đã lưu full url thì trả luôn
            if (externalId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                externalId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return externalId;

            // mặc định: facebook marketplace item link
            return $"https://www.facebook.com/marketplace/item/{externalId.Trim()}";
        }

        public async Task<(IReadOnlyList<ListingListItemDto> items, int total)> ListAsync(
            int? keywordId, string? q, bool? isActive, DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.Listings.AsNoTracking().AsQueryable();

            if (keywordId is not null) query = query.Where(x => x.KeywordId == keywordId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim().ToLowerInvariant();
                query = query.Where(x => (x.Title ?? "").ToLower().Contains(k));
            }

            if (isActive is not null) query = query.Where(x => (x.IsActive ?? true) == isActive.Value);
            if (from is not null) query = query.Where(x => x.FirstSeen >= from);
            if (to is not null) query = query.Where(x => x.LastSeen <= to);

            var total = await query.CountAsync(ct);

            // ✅ lấy ExternalId để build link
            var raw = await query
                .OrderByDescending(x => x.LastSeen)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Price,
                    x.Currency,
                    x.Location,
                    x.IsActive,
                    x.FirstSeen,
                    x.LastSeen,
                    x.ExternalId
                })
                .ToListAsync(ct);

            var items = raw
                .Select(x => new ListingListItemDto(
                    x.Id,
                    x.Title,
                    (decimal?)x.Price,
                    x.Currency,
                    x.Location,
                    x.IsActive ?? true,
                    x.FirstSeen ?? x.LastSeen ?? DateTimeOffset.UtcNow,
                    x.LastSeen ?? x.FirstSeen ?? DateTimeOffset.UtcNow,
                    BuildLink(x.ExternalId)
                ))
                .ToList();

            return (items, total);
        }

        public async Task<ListingDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var x = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
            if (x is null) return null;
            return new ListingDetailDto(
                x.Id, x.ExternalId, x.Title, (decimal?)x.Price, x.Currency, x.Location, x.Condition,
                x.IsActive ?? true, x.FirstSeen ?? x.LastSeen ?? DateTimeOffset.UtcNow,
                x.LastSeen ?? x.FirstSeen ?? DateTimeOffset.UtcNow);
        }

        public async Task<IReadOnlyList<ListingChangeDto>> GetChangesAsync(Guid id, CancellationToken ct = default)
        {
            var list = await _db.ListingChanges.AsNoTracking()
                .Where(c => c.ListingId == id)
                .OrderByDescending(c => c.OccurredAt)
                .Select(c => new ListingChangeDto(
                    c.Id, c.ChangeType,
                    c.OldValue == null ? null : c.OldValue.ToString(),
                    c.NewValue == null ? null : c.NewValue.ToString(),
                    c.OccurredAt ?? DateTimeOffset.UtcNow))
                .ToListAsync(ct);
            return list;
        }
    }
}
