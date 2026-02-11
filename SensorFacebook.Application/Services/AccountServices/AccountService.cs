using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices
{
    public sealed class AccountService : IAccountService
    {
        // ✅ Ràng buộc bằng code: chỉ cho phép đúng 4 trạng thái này
        private static readonly HashSet<AccountStatus> AllowedStatuses = new()
        {
            AccountStatus.Active,
            AccountStatus.Suspended,
            AccountStatus.Checkpointed,
            AccountStatus.Disabled
        };

        // ✅ Map input user (string) -> enum (strict)
        // Hỗ trợ alias để UI/legacy không bị gãy
        // - "locked" => inactive
        // - "suspended" => inactive
        // - "checkpointed" => checkpoint
        private static AccountStatus ParseStatusOrDefault(string? s, AccountStatus defaultValue = AccountStatus.Active)
        {
            if (string.IsNullOrWhiteSpace(s))
                return defaultValue;

            var raw = s.Trim().ToLowerInvariant();

            // alias mapping (tuỳ bạn muốn hỗ trợ đến mức nào)
            raw = raw switch
            {
                "locked" => "inactive",
                "suspended" => "inactive",
                "checkpointed" => "checkpoint",
                _ => raw
            };

            // parse enum theo tên value trong enum
            if (!Enum.TryParse<AccountStatus>(raw, ignoreCase: true, out var v))
                throw new ArgumentException("Invalid status. Allowed: active|inactive|checkpoint|disabled");

            if (!AllowedStatuses.Contains(v))
                throw new ArgumentException("Invalid status. Allowed: active|inactive|checkpoint|disabled");

            return v;
        }

        private readonly SensorDbContext _db;
        private readonly ICookieCryptoService _crypto;

        public AccountService(SensorDbContext db, ICookieCryptoService crypto)
        {
            _db = db;
            _crypto = crypto;
        }

        public async Task<(IReadOnlyList<FbAccountDto> items, int total)> ListAsync(
            int page,
            int pageSize,
            string? q,
            string? status,
            string? region,
            CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query =
                from a in _db.FbAccounts.AsNoTracking()
                join pg in _db.ProxyGroups.AsNoTracking() on a.ProxyGroupId equals pg.Id into gp
                from pg in gp.DefaultIfEmpty()
                select new { a, pg };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.a.Email.ToLower().Contains(k) ||
                    (x.a.DisplayName ?? "").ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = ParseStatusOrDefault(status);
                query = query.Where(x => x.a.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                var r = region.Trim().ToLowerInvariant();
                query = query.Where(x => (x.pg.Region ?? "").ToLower() == r);
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FbAccountDto(
                    x.a.Id,
                    x.a.Email,
                    x.a.DisplayName,
                    x.a.ProxyGroupId,
                    x.pg != null ? x.pg.Name : null,
                    x.pg != null ? x.pg.Region : null,
                    x.a.Status,
                    x.a.CheckpointCount ?? 0,
                    x.a.LastCheckpoint,
                    x.a.CreatedBy,
                    x.a.CreatedAt,
                    x.a.EncryptedCookie != null,
                    x.a.ProfileDir
                ))
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<FbAccountDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var row = await (
                from a in _db.FbAccounts.AsNoTracking()
                join pg in _db.ProxyGroups.AsNoTracking() on a.ProxyGroupId equals pg.Id into gp
                from pg in gp.DefaultIfEmpty()
                where a.Id == id
                select new { a, pg }
            ).FirstOrDefaultAsync(ct);

            if (row is null) return null;

            return new FbAccountDto(
                row.a.Id,
                row.a.Email,
                row.a.DisplayName,
                row.a.ProxyGroupId,
                row.pg?.Name,
                row.pg?.Region,
                row.a.Status,
                row.a.CheckpointCount ?? 0,
                row.a.LastCheckpoint,
                row.a.CreatedBy,
                row.a.CreatedAt,
                row.a.EncryptedCookie != null,
                row.a.ProfileDir
            );
        }

        public async Task<Guid> CreateOrUpdateAsync(CreateOrUpdateAccountRequest req, Guid? currentUserId, CancellationToken ct = default)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));

            if (string.IsNullOrWhiteSpace(req.Email))
                throw new ArgumentException("Email is required");

            var email = req.Email.Trim();

            // Validate proxy group nếu có
            if (req.ProxyGroupId is not null)
            {
                var exists = await _db.ProxyGroups.AnyAsync(x => x.Id == req.ProxyGroupId, ct);
                if (!exists) throw new ArgumentException("Proxy group not found");
            }

            // ✅ status default active
            var parsedStatus = ParseStatusOrDefault(req.Status, AccountStatus.Active);

            if (req.Id is null)
            {
                // CREATE
                var dupe = await _db.FbAccounts.AnyAsync(x => x.Email == email, ct);
                if (dupe) throw new ArgumentException("Email already exists");

                var entity = new FbAccount
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    DisplayName = req.DisplayName?.Trim(),
                    ProxyGroupId = req.ProxyGroupId,
                    ProfileDir = req.ProfileDir,
                    Status = parsedStatus,
                    CheckpointCount = 0,
                    LastCheckpoint = null,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                if (!string.IsNullOrWhiteSpace(req.CookiePlain))
                    entity.EncryptedCookie = _crypto.Encrypt(req.CookiePlain!);

                _db.FbAccounts.Add(entity);
                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
            else
            {
                // UPDATE
                var entity = await _db.FbAccounts.FirstOrDefaultAsync(x => x.Id == req.Id.Value, ct);
                if (entity is null) throw new ArgumentException("Account not found");

                // Nếu email đổi, check trùng
                if (!string.Equals(entity.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var dupe = await _db.FbAccounts.AnyAsync(x => x.Email == email && x.Id != entity.Id, ct);
                    if (dupe) throw new ArgumentException("Email already exists");
                    entity.Email = email;
                }

                entity.DisplayName = req.DisplayName?.Trim();
                entity.ProxyGroupId = req.ProxyGroupId;
                entity.ProfileDir = req.ProfileDir;

                // ✅ chỉ update status khi client truyền
                if (req.Status is not null)
                    entity.Status = ParseStatusOrDefault(req.Status);

                // ✅ chỉ update cookie khi client truyền
                if (!string.IsNullOrWhiteSpace(req.CookiePlain))
                    entity.EncryptedCookie = _crypto.Encrypt(req.CookiePlain!);

                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
        }

        public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
        {
            var st = ParseStatusOrDefault(status);

            var a = await _db.FbAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (a is null) return false;

            a.Status = st;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // ✅ Lock = inactive (bị khoá)
        public Task<bool> LockAsync(Guid id, CancellationToken ct = default)
            => UpdateStatusAsync(id, "inactive", ct);

        // ✅ Unlock = active
        public Task<bool> UnlockAsync(Guid id, CancellationToken ct = default)
            => UpdateStatusAsync(id, "active", ct);

        public async Task<IReadOnlyList<AccountEventDto>> GetEventsAsync(
            Guid id,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var accExists = await _db.FbAccounts.AnyAsync(x => x.Id == id, ct);
            if (!accExists) return Array.Empty<AccountEventDto>();

            var items = await _db.AccountEvents.AsNoTracking()
                .Where(x => x.AccountId == id)
                .OrderByDescending(x => x.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AccountEventDto(
                    x.Id,
                    x.AccountId ?? Guid.Empty,
                    x.EventType,
                    x.Payload,
                    x.OccurredAt
                ))
                .ToListAsync(ct);

            return items;
        }
    }
}
