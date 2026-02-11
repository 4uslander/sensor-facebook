using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.LocationServices;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Abstractions;
using SensorFacebook.Shared.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SensorFacebook.Application.Services.KeywordServices;

public sealed class KeywordService : IKeywordService
{
    private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    { "relevance", "distance_asc", "date_desc", "price_asc", "price_desc" };

    private static readonly HashSet<string> AllowedCondition = new(StringComparer.OrdinalIgnoreCase)
    { "new", "like_new", "good", "fair" };

    private static readonly HashSet<string> AllowedListedTime = new(StringComparer.OrdinalIgnoreCase)
    { "all", "24h", "7d", "30d" };

    private static readonly HashSet<string> AllowedAvailability = new(StringComparer.OrdinalIgnoreCase)
    { "available", "sold" };

    private readonly SensorDbContext _db;
    private readonly IRadiusNormalizer _normalizer;
    private readonly IBusPublisher _bus;

    public KeywordService(
        SensorDbContext db,
        IRadiusNormalizer normalizer,
        IBusPublisher bus)
    {
        _db = db;
        _normalizer = normalizer;
        _bus = bus;
    }

    public async Task<(IReadOnlyList<KeywordDto> items, int total)> ListAsync(
        int page,
        int pageSize,
        string? q,
        int? categoryId,
        bool? active,
        string? sortBy = null,
        IEnumerable<string>? conditions = null,
        string? listedTime = null,
        string? availability = null,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;

        var query = _db.Keywords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim().ToLowerInvariant();
            query = query.Where(x => x.Text.ToLower().Contains(k));
        }

        if (categoryId is not null)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        if (active is not null)
            query = query.Where(x => (x.Active ?? true) == active.Value);

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var s = NormalizeSortBy(sortBy);
            query = query.Where(x => (x.SortBy ?? "relevance").ToLower() == s);
        }

        if (!string.IsNullOrWhiteSpace(listedTime))
        {
            var lt = NormalizeListedTime(listedTime);
            query = query.Where(x => (x.ListedTime ?? "all").ToLower() == lt);
        }

        if (!string.IsNullOrWhiteSpace(availability))
        {
            var av = NormalizeAvailabilityOrNull(availability);
            if (av is not null)
                query = query.Where(x => (x.Availability ?? "available").ToLower() == av);
        }

        if (conditions is not null)
        {
            var conds = NormalizeConditions(conditions);
            if (conds is not null && conds.Length > 0)
            {
                foreach (var c in conds)
                {
                    var cLocal = c;
                    query = query.Where(x => x.Conditions != null && x.Conditions.Contains(cLocal));
                }
            }
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new KeywordDto(
                x.Id,
                x.Text,
                x.CategoryId,
                x.Priority ?? 1,
                x.Active ?? true,
                x.LocationLat,
                x.LocationLon,
                x.RadiusKm,
                x.RadiusPolicy ?? "platform",
                x.SortBy ?? "relevance",
                x.Conditions,
                x.ListedTime ?? "all",
                x.Availability ?? "available",
                x.CreatedAt
            ))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<KeywordDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var k = await _db.Keywords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        return k is null ? null : new KeywordDto(
            k.Id,
            k.Text,
            k.CategoryId,
            k.Priority ?? 1,
            k.Active ?? true,
            k.LocationLat,
            k.LocationLon,
            k.RadiusKm,
            k.RadiusPolicy ?? "platform",
            k.SortBy ?? "relevance",
            k.Conditions,
            k.ListedTime ?? "all",
            k.Availability ?? "available",
            k.CreatedAt
        );
    }

    public async Task<int> CreateAsync(CreateKeywordRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            throw new ArgumentException("Text is required");

        var text = req.Text.Trim();

        if ((req.LocationLat.HasValue && !req.LocationLon.HasValue) ||
            (!req.LocationLat.HasValue && req.LocationLon.HasValue))
            throw new ArgumentException("Both LocationLat and LocationLon must be provided together.");

        if (req.LocationLat is not null && (req.LocationLat < -90 || req.LocationLat > 90))
            throw new ArgumentOutOfRangeException(nameof(req.LocationLat));

        if (req.LocationLon is not null && (req.LocationLon < -180 || req.LocationLon > 180))
            throw new ArgumentOutOfRangeException(nameof(req.LocationLon));

        var policy = NormalizePolicy(req.RadiusPolicy);
        var normalizedKm = await _normalizer.NormalizeForFacebookAsync(req.RadiusKm, policy, ct);

        var sortBy = NormalizeSortBy(req.SortBy);
        var listedTime = NormalizeListedTime(req.ListedTime);
        var availability = NormalizeAvailability(req.Availability);
        var conditions = NormalizeConditions(req.Conditions);

        var entity = new Keyword
        {
            Text = text,
            CategoryId = req.CategoryId,
            Priority = req.Priority ?? 1,
            Active = req.Active ?? true,
            LocationLat = req.LocationLat,
            LocationLon = req.LocationLon,
            RadiusKm = normalizedKm,

            RadiusPolicy = policy,
            SortBy = sortBy,
            ListedTime = listedTime,
            Availability = availability,
            Conditions = conditions,

            CreatedAt = DateTime.UtcNow
        };

        _db.Keywords.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, UpdateKeywordRequest req, CancellationToken ct = default)
    {
        var k = await _db.Keywords.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (k is null) return false;

        if (req.Text is not null)
        {
            var t = req.Text.Trim();
            if (string.IsNullOrWhiteSpace(t)) throw new ArgumentException("Text cannot be empty");
            k.Text = t;
        }

        if (req.CategoryId is not null) k.CategoryId = req.CategoryId;
        if (req.Priority is not null) k.Priority = req.Priority.Value;
        if (req.Active is not null) k.Active = req.Active.Value;

        if (req.LocationLat is not null || req.LocationLon is not null)
        {
            if ((req.LocationLat.HasValue && !req.LocationLon.HasValue) ||
                (!req.LocationLat.HasValue && req.LocationLon.HasValue))
                throw new ArgumentException("Both LocationLat and LocationLon must be provided together.");

            if (req.LocationLat is not null && (req.LocationLat < -90 || req.LocationLat > 90))
                throw new ArgumentOutOfRangeException(nameof(req.LocationLat));

            if (req.LocationLon is not null && (req.LocationLon < -180 || req.LocationLon > 180))
                throw new ArgumentOutOfRangeException(nameof(req.LocationLon));

            k.LocationLat = req.LocationLat;
            k.LocationLon = req.LocationLon;
        }

        if (req.RadiusPolicy is not null || req.RadiusKm is not null)
        {
            var policy = NormalizePolicy(req.RadiusPolicy ?? k.RadiusPolicy);
            var normalizedKm = await _normalizer.NormalizeForFacebookAsync(req.RadiusKm ?? k.RadiusKm, policy, ct);
            k.RadiusPolicy = policy;
            k.RadiusKm = normalizedKm;
        }

        if (req.SortBy is not null) k.SortBy = NormalizeSortBy(req.SortBy);
        if (req.ListedTime is not null) k.ListedTime = NormalizeListedTime(req.ListedTime);
        if (req.Availability is not null) k.Availability = NormalizeAvailability(req.Availability);
        if (req.Conditions is not null) k.Conditions = NormalizeConditions(req.Conditions);

        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var k = await _db.Keywords.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (k is null) return false;

        _db.Keywords.Remove(k);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    // ---------- helpers ----------
    private static string NormalizePolicy(string? input)
    {
        var p = (input ?? "platform").Trim().ToLowerInvariant();
        return p switch
        {
            "auto" => "auto",
            "fixed" => "fixed",
            "platform" => "platform",
            _ => "platform"
        };
    }

    private static string NormalizeSortBy(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "relevance";
        var s = input.Trim().ToLowerInvariant();
        return AllowedSortBy.Contains(s) ? s : "relevance";
    }

    private static string NormalizeListedTime(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "all";
        var s = input.Trim().ToLowerInvariant();
        return AllowedListedTime.Contains(s) ? s : "all";
    }

    private static string NormalizeAvailability(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "available";
        var s = input.Trim().ToLowerInvariant();
        return AllowedAvailability.Contains(s) ? s : "available";
    }

    private static string? NormalizeAvailabilityOrNull(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim().ToLowerInvariant();
        return AllowedAvailability.Contains(s) ? s : null;
    }

    private static string[]? NormalizeConditions(IEnumerable<string>? inputs)
    {
        if (inputs is null) return null;

        var list = inputs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => AllowedCondition.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return list.Length == 0 ? Array.Empty<string>() : list;
    }

    public async Task<Guid> EnqueueSearchAsync(int keywordId, string priority = "low", CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var job = new SearchJob
        {
            Id = Guid.NewGuid(),
            KeywordId = keywordId,
            Status = JobStatus.queued,
            ScheduledAt = now
        };

        _db.SearchJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        var nowOffset = new DateTimeOffset(now, TimeSpan.Zero);
        var msg = new SearchJobMsg(
            job.Id,
            keywordId,
            null,
            null,
            priority,
            now,
            $"kw:{keywordId}:{nowOffset.ToUnixTimeSeconds() / 180}"
        );

        await _bus.PublishAsync(priority == "high" ? "search.high" : "search.low", msg, ct);
        return job.Id;
    }
}
