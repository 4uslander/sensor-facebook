using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.LocationServices;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;

namespace SensorFacebook.Application.Services.KeywordServices
{
    public sealed class KeywordImportExportService : IKeywordImportExportService
    {
        private readonly SensorDbContext _db;
        private readonly IRadiusNormalizer _normalizer;

        // đồng bộ với KeywordService
        private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    { "relevance", "distance_asc", "date_desc", "price_asc", "price_desc" };

        private static readonly HashSet<string> AllowedCondition = new(StringComparer.OrdinalIgnoreCase)
    { "new", "like_new", "good", "fair" };

        private static readonly HashSet<string> AllowedListedTime = new(StringComparer.OrdinalIgnoreCase)
    { "all", "24h", "7d", "30d" };

        private static readonly HashSet<string> AllowedAvailability = new(StringComparer.OrdinalIgnoreCase)
    { "available", "sold" };

        public KeywordImportExportService(SensorDbContext db, IRadiusNormalizer normalizer)
        {
            _db = db; _normalizer = normalizer;
        }

        // ------------------ EXPORT ------------------
        public async Task<Stream> ExportCsvAsync(
    string? q, int? categoryId, bool? active,
    string? sortBy, IEnumerable<string>? conditions, string? listedTime, string? availability,
    CancellationToken ct = default)
        {
            var query = _db.Keywords.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim().ToLowerInvariant();
                query = query.Where(x => x.Text.ToLower().Contains(k));
            }
            if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId.Value);
            if (active is not null) query = query.Where(x => (x.Active ?? true) == active.Value);

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var s = sortBy.Trim().ToLowerInvariant();
                query = query.Where(x => (x.SortBy ?? "relevance").ToLower() == s);
            }
            if (!string.IsNullOrWhiteSpace(listedTime))
            {
                var lt = listedTime.Trim().ToLowerInvariant();
                query = query.Where(x => (x.ListedTime ?? "all").ToLower() == lt);
            }
            if (!string.IsNullOrWhiteSpace(availability))
            {
                var av = availability.Trim().ToLowerInvariant();
                query = query.Where(x => (x.Availability ?? "available").ToLower() == av);
            }
            if (conditions is not null)
            {
                var conds = conditions.Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Select(s => s.Trim().ToLowerInvariant()).Distinct().ToArray();
                foreach (var c in conds)
                {
                    var cLocal = c;
                    query = query.Where(x => x.Conditions != null && x.Conditions.Contains(cLocal));
                }
            }

            // 1) Materialize RAW fields ONLY (no null-propagation, no string.Join, no CultureInfo)
            var raw = await query
                .OrderBy(x => x.Text)
                .Select(x => new {
                    x.Text,
                    x.CategoryId,
                    x.Priority,
                    Active = x.Active,
                    x.LocationLat,
                    x.LocationLon,
                    x.RadiusKm,
                    RadiusPolicy = x.RadiusPolicy,
                    SortBy = x.SortBy,
                    ListedTime = x.ListedTime,
                    Availability = x.Availability,
                    Conditions = x.Conditions
                })
                .ToListAsync(ct);

            // 2) Format in memory
            var list = raw.Select(x => new KeywordCsvRow
            {
                Text = x.Text,
                CategoryId = x.CategoryId,
                Priority = x.Priority,
                Active = (x.Active ?? true) ? "true" : "false",
                LocationLat = x.LocationLat,
                LocationLon = x.LocationLon,
                Location = (x.LocationLat is not null && x.LocationLon is not null)
                                ? $"{x.LocationLat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {x.LocationLon.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                                : null,
                RadiusKm = x.RadiusKm,
                RadiusPolicy = x.RadiusPolicy ?? "platform",
                SortBy = x.SortBy ?? "relevance",
                ListedTime = x.ListedTime ?? "all",
                Availability = x.Availability ?? "available",
                Conditions = (x.Conditions is null || x.Conditions.Length == 0) ? null : string.Join(";", x.Conditions)
            }).ToList();

            var ms = new MemoryStream();
            using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
            using (var csv = new CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            { NewLine = Environment.NewLine }))
            {
                csv.WriteHeader<KeywordCsvRow>();
                await csv.NextRecordAsync();
                foreach (var row in list)
                {
                    csv.WriteRecord(row);
                    await csv.NextRecordAsync();
                }
            }
            ms.Position = 0;
            return ms;
        }

        // ------------------ IMPORT ------------------
        public async Task<KeywordImportResult> ImportCsvAsync(Stream csvStream, CancellationToken ct = default)
        {
            var result = new KeywordImportResult();

            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            using var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null
            });

            // Read header first to make row numbers predictable
            if (!await csv.ReadAsync()) return result;
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var rowNumber = csv.Context.Parser.Row;
                var raw = csv.Context.Parser.RawRecord;

                try
                {
                    var rec = csv.GetRecord<KeywordCsvRow>();
                    if (rec is null) throw new Exception("Empty row");

                    // ---- validate & parse ----
                    if (string.IsNullOrWhiteSpace(rec.Text))
                        throw new Exception("Text is required");

                    // Active
                    bool active = true;
                    if (!string.IsNullOrWhiteSpace(rec.Active))
                    {
                        var s = rec.Active.Trim().ToLowerInvariant();
                        active = s is "true" or "1" or "yes" or "y";
                    }

                    // Location
                    decimal? lat = rec.LocationLat, lon = rec.LocationLon;
                    if (!string.IsNullOrWhiteSpace(rec.Location) && (lat is null || lon is null))
                    {
                        var parts = rec.Location.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2 &&
                            decimal.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var la) &&
                            decimal.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lo))
                        { lat = la; lon = lo; }
                    }
                    if ((lat.HasValue && !lon.HasValue) || (!lat.HasValue && lon.HasValue))
                        throw new Exception("Both LocationLat and LocationLon must be provided together.");
                    if (lat is not null && (lat < -90 || lat > 90)) throw new Exception("LocationLat out of range");
                    if (lon is not null && (lon < -180 || lon > 180)) throw new Exception("LocationLon out of range");

                    // Policy & radius
                    var policy = NormalizePolicy(rec.RadiusPolicy);
                    var normalizedKm = await _normalizer.NormalizeForFacebookAsync(rec.RadiusKm, policy, ct);

                    // 4 filters
                    var sortBy = NormalizeSortBy(rec.SortBy) ?? "relevance";
                    var listedTime = NormalizeListedTime(rec.ListedTime) ?? "all";
                    var availabilityEnum = NormalizeAvailability(rec.Availability);
                    var condArray = NormalizeConditions(SplitMulti(rec.Conditions));

                    // Upsert by (Text + CategoryId)
                    var text = rec.Text.Trim();
                    var existing = await _db.Keywords.FirstOrDefaultAsync(
                        x => x.Text.ToLower() == text.ToLower() && x.CategoryId == rec.CategoryId, ct);

                    if (existing is null)
                    {
                        var entity = new Keyword
                        {
                            Text = text,
                            CategoryId = rec.CategoryId,
                            Priority = rec.Priority ?? 1,
                            Active = active,
                            LocationLat = lat,
                            LocationLon = lon,
                            RadiusKm = normalizedKm,
                            RadiusPolicy = policy,
                            SortBy = sortBy,
                            ListedTime = listedTime,
                            Availability = NormalizeAvailability(rec.Availability),
                            Conditions = condArray,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Keywords.Add(entity);
                        result.Created++;
                    }
                    else
                    {
                        existing.Priority = rec.Priority ?? existing.Priority ?? 1;
                        existing.Active = active;
                        existing.LocationLat = lat;
                        existing.LocationLon = lon;
                        existing.RadiusKm = normalizedKm;
                        existing.RadiusPolicy = policy;
                        existing.SortBy = sortBy;
                        existing.ListedTime = listedTime;
                        existing.Availability = NormalizeAvailability(rec.Availability);
                        existing.Conditions = condArray;
                        result.Updated++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new KeywordImportError
                    {
                        RowNumber = rowNumber,
                        Error = ex.Message,
                        Raw = raw
                    });
                }
            }

            result.Total = result.Created + result.Updated + result.Failed;
            result.Failed = result.Errors.Count;
            await _db.SaveChangesAsync(ct);
            return result;
        }


        // -------- helpers --------
        private static string NormalizePolicy(string? input)
        {
            var p = (input ?? "platform").Trim().ToLowerInvariant();
            return p switch { "auto" => "auto", "fixed" => "fixed", _ => "platform" };
        }

        private static string? NormalizeSortBy(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = input.Trim().ToLowerInvariant();
            return AllowedSortBy.Contains(s) ? s : null;
        }

        private static string? NormalizeListedTime(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = input.Trim().ToLowerInvariant();
            return AllowedListedTime.Contains(s) ? s : null;
        }

        private static string NormalizeAvailability(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "available";

            var s = input.Trim().ToLowerInvariant();
            return AllowedAvailability.Contains(s) ? s : "available";
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

        private static IEnumerable<string>? SplitMulti(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            // chấp nhận phân tách bằng ; hoặc ,
            return s.Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
