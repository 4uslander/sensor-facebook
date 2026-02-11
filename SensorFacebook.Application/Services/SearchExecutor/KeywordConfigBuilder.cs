using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.LocationServices;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.SearchExecutor
{
    public interface IKeywordConfigBuilder
    {
        Task<KeywordConfig> BuildAsync(int keywordId, CancellationToken ct = default);
    }

    public sealed class KeywordConfigBuilder : IKeywordConfigBuilder
    {
        private readonly SensorDbContext _db;
        private readonly IRadiusNormalizer _normalizer;

        public KeywordConfigBuilder(SensorDbContext db, IRadiusNormalizer normalizer)
        {
            _db = db; _normalizer = normalizer;
        }

        public async Task<KeywordConfig> BuildAsync(int keywordId, CancellationToken ct = default)
        {
            var k = await _db.Keywords.AsNoTracking()
                .Where(x => x.Id == keywordId)
                .Select(x => new {
                    x.Text,
                    x.LocationLat,
                    x.LocationLon,
                    x.RadiusKm,
                    x.RadiusPolicy,
                    x.SortBy,
                    x.Conditions,
                    x.ListedTime,
                    x.Availability
                })
                .FirstOrDefaultAsync(ct);

            if (k is null) throw new ArgumentException("Keyword not found");

            var policy = (k.RadiusPolicy ?? "platform").Trim().ToLowerInvariant();
            var normalized = await _normalizer.NormalizeForFacebookAsync(k.RadiusKm, policy, ct);

            var conditions = (k.Conditions ?? Array.Empty<string>()).Select(s => s.Trim().ToLowerInvariant()).ToArray();

            return new KeywordConfig(
                Q: (k.Text ?? "").Trim(),
                LocationLat: (double?)k.LocationLat,
                LocationLon: (double?)k.LocationLon,
                RadiusKm: normalized,
                SortBy: (k.SortBy ?? "relevance").Trim().ToLowerInvariant(),
                Conditions: conditions,
                ListedTime: (k.ListedTime ?? "all").Trim().ToLowerInvariant(),
                Availability: (k.Availability ?? "available").Trim().ToLowerInvariant()
            );
        }
    }
}
