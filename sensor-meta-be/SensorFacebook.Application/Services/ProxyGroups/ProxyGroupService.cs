using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.AccountServices.Security; // AES-GCM (cookie) để mã hoá password proxy
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public sealed class ProxyGroupService : IProxyGroupService
    {
        private readonly SensorDbContext _db;
        private readonly ICookieCryptoService _crypto;

        private static readonly HashSet<string> AllowedProtocols = new(StringComparer.OrdinalIgnoreCase)
        { "http", "https", "socks4", "socks5" };

        public ProxyGroupService(SensorDbContext db, ICookieCryptoService crypto)
        {
            _db = db;
            _crypto = crypto;
        }

        public async Task<(IReadOnlyList<ProxyGroupDto> items, int total)> ListAsync(
    int page, int pageSize, string? q, string? status, string? region, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.ProxyGroups.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.ToLower().Trim();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(k) ||
                    (x.Region ?? string.Empty).ToLower().Contains(k) ||
                    (x.Host ?? string.Empty).ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.Status == status);

            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(x => x.Region == region);

            var total = await query.CountAsync(ct);

            var itemsRaw = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(pg => new
                {
                    pg.Id,
                    pg.Name,
                    pg.Region,
                    pg.Status,
                    pg.Protocol,
                    pg.Host,
                    pg.Port,
                    pg.AuthUsername,
                    pg.AuthPasswordEnc,
                    pg.Provider,
                    pg.IsRotating,
                    pg.MaxConcurrency,
                    pg.RateLimitRpm,
                    pg.Metadata, // ✅ string json trong DB

                    pg.LastChecked,
                    pg.LastOkAt,
                    pg.SuccessCount,
                    pg.FailCount,

                    LastHealth = _db.ProxyHealths
                        .Where(h => h.ProxyGroupId == pg.Id)
                        .OrderByDescending(h => h.CheckedAt)
                        .Select(h => new { h.LatencyMs, h.LastStatus })
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            JsonElement? ParseMetadata(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                try
                {
                    using var doc = JsonDocument.Parse(s);
                    return doc.RootElement.Clone();
                }
                catch
                {
                    return null; // metadata bẩn => trả null để không làm crash list
                }
            }

            var items = itemsRaw.Select(x =>
                new ProxyGroupDto(
                    x.Id,
                    x.Name,
                    x.Region,
                    x.Status ?? "active",
                    x.Protocol,
                    x.Host,
                    x.Port,
                    HasAuth: !string.IsNullOrWhiteSpace(x.AuthUsername) && !string.IsNullOrWhiteSpace(x.AuthPasswordEnc),
                    AuthUsername: x.AuthUsername,                 // ✅
                    x.Provider,
                    x.IsRotating,
                    x.MaxConcurrency,
                    x.RateLimitRpm,
                    MetadataJson: ParseMetadata(x.Metadata),      // ✅
                    x.LastChecked,
                    x.LastOkAt,
                    x.SuccessCount,
                    x.FailCount,
                    x.LastHealth?.LatencyMs,
                    x.LastHealth?.LastStatus,
                    Endpoint: BuildEndpoint(x.Protocol, x.Host, x.Port)
                )
            ).ToList();

            return (items, total);
        }

        public async Task<ProxyGroupDto?> GetAsync(int id, CancellationToken ct = default)
        {
            var pg = await _db.ProxyGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (pg is null) return null;

            var latest = await _db.ProxyHealths.AsNoTracking()
                .Where(h => h.ProxyGroupId == id)
                .OrderByDescending(h => h.CheckedAt)
                .FirstOrDefaultAsync(ct);

            JsonElement? metadataJson = null;
            if (!string.IsNullOrWhiteSpace(pg.Metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(pg.Metadata);
                    metadataJson = doc.RootElement.Clone();
                }
                catch
                {
                    metadataJson = null;
                }
            }

            return new ProxyGroupDto(
                pg.Id,
                pg.Name,
                pg.Region,
                pg.Status ?? "active",
                pg.Protocol,
                pg.Host,
                pg.Port,
                HasAuth: !string.IsNullOrWhiteSpace(pg.AuthUsername) && !string.IsNullOrWhiteSpace(pg.AuthPasswordEnc),
                AuthUsername: pg.AuthUsername,         // ✅
                pg.Provider,
                pg.IsRotating,
                pg.MaxConcurrency,
                pg.RateLimitRpm,
                MetadataJson: metadataJson,            // ✅
                pg.LastChecked,
                pg.LastOkAt,
                pg.SuccessCount,
                pg.FailCount,
                latest?.LatencyMs,
                latest?.LastStatus,
                Endpoint: BuildEndpoint(pg.Protocol, pg.Host, pg.Port)
            );
        }


        public async Task<int> CreateAsync(CreateProxyGroupRequest req, CancellationToken ct = default)
        {
            ValidateEndpoint(req.Protocol, req.Host, req.Port);
            var status = ProxyStatus.NormalizeOrDefault(req.Status);

            var entity = new Infrastructure.Entities.ProxyGroup
            {
                Name = req.Name.Trim(),
                Region = string.IsNullOrWhiteSpace(req.Region) ? null : req.Region.Trim(),
                Status = status,

                Protocol = req.Protocol.Trim().ToLowerInvariant(),
                Host = req.Host.Trim(),
                Port = req.Port,

                AuthUsername = string.IsNullOrWhiteSpace(req.AuthUsername) ? null : req.AuthUsername.Trim(),
                AuthPasswordEnc = string.IsNullOrWhiteSpace(req.AuthPasswordPlain) ? null : _crypto.Encrypt(req.AuthPasswordPlain),

                Provider = string.IsNullOrWhiteSpace(req.Provider) ? null : req.Provider.Trim(),
                IsRotating = req.IsRotating ?? false,
                MaxConcurrency = req.MaxConcurrency ?? 3,
                RateLimitRpm = req.RateLimitRpm,

                Metadata = NormalizeJsonOrNull(req.MetadataJson)
            };

            _db.ProxyGroups.Add(entity);
            await _db.SaveChangesAsync(ct);

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, UpdateProxyGroupRequest req, CancellationToken ct = default)
        {
            var pg = await _db.ProxyGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (pg is null) return false;

            if (req.Name is not null) pg.Name = req.Name.Trim();
            if (req.Region is not null) pg.Region = string.IsNullOrWhiteSpace(req.Region) ? null : req.Region.Trim();

            if (req.Status is not null)
                pg.Status = ProxyStatus.NormalizeOrKeep(req.Status);

            if (req.Protocol is not null || req.Host is not null || req.Port is not null)
            {
                var protocol = req.Protocol?.Trim().ToLowerInvariant() ?? pg.Protocol;
                var host = req.Host?.Trim() ?? pg.Host;
                var port = req.Port ?? pg.Port;
                ValidateEndpoint(protocol, host, port);

                pg.Protocol = protocol;
                pg.Host = host;
                pg.Port = port;
            }

            if (req.AuthUsername is not null)
                pg.AuthUsername = string.IsNullOrWhiteSpace(req.AuthUsername) ? null : req.AuthUsername.Trim();

            if (!string.IsNullOrWhiteSpace(req.AuthPasswordPlain))
                pg.AuthPasswordEnc = _crypto.Encrypt(req.AuthPasswordPlain);

            if (req.Provider is not null)
                pg.Provider = string.IsNullOrWhiteSpace(req.Provider) ? null : req.Provider.Trim();

            if (req.IsRotating is not null) pg.IsRotating = req.IsRotating;
            if (req.MaxConcurrency is not null) pg.MaxConcurrency = req.MaxConcurrency;
            if (req.RateLimitRpm is not null) pg.RateLimitRpm = req.RateLimitRpm;

            if (req.MetadataJson is not null)
                pg.Metadata = NormalizeJsonOrNull(req.MetadataJson);

            return await _db.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var pg = await _db.ProxyGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (pg is null) return false;

            _db.ProxyGroups.Remove(pg);
            return await _db.SaveChangesAsync(ct) > 0;
        }

        // ---------------- helpers ----------------

        private static string? NormalizeJsonOrNull(JsonElement? el)
        {
            if (el is null) return null;
            if (el.Value.ValueKind == JsonValueKind.Null) return null;
            return el.Value.GetRawText();
        }

        private static void ValidateEndpoint(string? protocol, string? host, int? port)
        {
            if (string.IsNullOrWhiteSpace(protocol) || string.IsNullOrWhiteSpace(host) || port is null)
                throw new ArgumentException("Protocol, Host and Port are required");

            if (!AllowedProtocols.Contains(protocol))
                throw new ArgumentException("Protocol must be http|https|socks4|socks5");

            if (port < 1 || port > 65535)
                throw new ArgumentException("Port must be in 1..65535");
        }

        private static string? BuildEndpoint(string? protocol, string? host, int? port)
        {
            if (string.IsNullOrWhiteSpace(protocol) || string.IsNullOrWhiteSpace(host) || port is null) return null;
            return $"{protocol}://{host}:{port}";
        }
    }
}
