using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices
{
    public sealed class AccountSelectorService : IAccountSelector
    {
        private readonly SensorDbContext _db;
        private readonly ILogger<AccountSelectorService> _log;
        private readonly AccountSelectorOptions _opt;

        public AccountSelectorService(
            SensorDbContext db,
            IOptions<AccountSelectorOptions> opt,
            ILogger<AccountSelectorService> log)
        {
            _db = db; _log = log; _opt = opt.Value;
        }

        public async Task<AccountLease> AcquireAsync(
            int requiredProxyGroupId,
            LeasePriority priority,
            string consumerKey,
            TimeSpan? ttl = null,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var leaseTtl = ttl ?? TimeSpan.FromSeconds(_opt.DefaultLeaseTtlSeconds);
            var expires = now.Add(leaseTtl);

            // 1) Capacity check per PG
            if (_opt.MaxConcurrentPerProxyGroup > 0)
            {
                var liveInPg = await _db.Sessions.AsNoTracking()
                    .CountAsync(s => s.ProxyGroupId == requiredProxyGroupId &&
                                     s.EndedAt == null &&
                                     s.ExpiresAt > now, ct);

                if (liveInPg >= _opt.MaxConcurrentPerProxyGroup)
                    throw new InvalidOperationException($"ProxyGroup {requiredProxyGroupId} reached capacity.");
            }

            // 2) Chọn candidates (status=active, không cooldown) dính proxy
            var qBase = _db.FbAccounts.AsNoTracking()
                .Where(a => a.Status == AccountStatus.Active &&
                            (a.CooldownUntil == null || a.CooldownUntil <= now) &&
                            (
                              a.PreferredProxyGroupId == requiredProxyGroupId ||
                              a.ProxyGroupId == requiredProxyGroupId
                            ));

            // 3) Lọc bỏ acc đang có session sống
            var liveAccIds = await _db.Sessions.AsNoTracking()
                .Where(s => s.EndedAt == null && s.ExpiresAt > now)
                .Select(s => s.AccountId)
                .Distinct()
                .ToListAsync(ct);

            var candidatesQuery = qBase.Where(a => !liveAccIds.Contains(a.Id));

            // 4) Fairness: ưu tiên preferred trước, sau đó LRU + round-robin Id
            var candidates = await candidatesQuery
                .Select(a => new
                {
                    a.Id,
                    a.ProxyGroupId,
                    a.PreferredProxyGroupId,
                    a.LastUsedAt
                })
                .OrderByDescending(a => a.PreferredProxyGroupId == requiredProxyGroupId) // preferred first
                .ThenBy(a => a.LastUsedAt ?? DateTimeOffset.MinValue)                    // LRU
                .ThenBy(a => a.Id)                                                       // round-robin đơn giản
                .Take(20)                                                                // giới hạn scan
                .ToListAsync(ct);

            var chosen = candidates.FirstOrDefault();
            if (chosen is null)
                throw new InvalidOperationException($"No available account for ProxyGroup {requiredProxyGroupId}");

            // 5) Tạo session (idempotent by consumerKey? => kiểm tra đã cầm chưa)
            var existing = await _db.Sessions.FirstOrDefaultAsync(s =>
                s.AccountId == chosen.Id &&
                s.EndedAt == null &&
                s.ExpiresAt > now &&
                s.ConsumerKey == consumerKey, ct);

            if (existing is not null)
            {
                // renew thay vì fail (idempotency)
                existing.ExpiresAt = expires;
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Renew existing session {SessionId} for {Account} by consumer={Consumer}",
                    existing.Id, chosen.Id, consumerKey);
                return new AccountLease(existing.Id, chosen.Id, requiredProxyGroupId, existing.ExpiresAt);
            }

            var session = new Session
            {
                Id = Guid.NewGuid(),
                AccountId = chosen.Id,
                ProxyGroupId = requiredProxyGroupId,
                StartedAt = now.UtcDateTime,
                ExpiresAt = expires.UtcDateTime,
                ConsumerKey = consumerKey
            };

            _db.Sessions.Add(session);

            // Update LastUsedAt để fairness
            var acc = await _db.FbAccounts.FirstAsync(a => a.Id == chosen.Id, ct);
            acc.LastUsedAt = now;

            await _db.SaveChangesAsync(ct);

            _log.LogInformation("Acquired account {Account} in PG {Pg} (prio={Prio}) session={Sid}",
                chosen.Id, requiredProxyGroupId, priority, session.Id);

            return new AccountLease(session.Id, chosen.Id, requiredProxyGroupId, expires);
        }

        public async Task<AccountLease> AcquireByAccountAsync(
            Guid accountId,
            int proxyGroupId,
            string consumerKey,
            TimeSpan? ttl = null,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var leaseTtl = ttl ?? TimeSpan.FromSeconds(_opt.DefaultLeaseTtlSeconds);
            var expires = now.Add(leaseTtl);

            var acc = await _db.FbAccounts.FirstOrDefaultAsync(a =>
                a.Id == accountId &&
                a.Status == AccountStatus.Active &&
                (a.CooldownUntil == null || a.CooldownUntil <= now) &&
                (a.PreferredProxyGroupId == proxyGroupId || a.ProxyGroupId == proxyGroupId), ct);

            if (acc is null) throw new InvalidOperationException("Account not available for this proxy group");

            // capacity check
            if (_opt.MaxConcurrentPerProxyGroup > 0)
            {
                var liveInPg = await _db.Sessions.AsNoTracking()
                    .CountAsync(s => s.ProxyGroupId == proxyGroupId && s.EndedAt == null && s.ExpiresAt > now, ct);
                if (liveInPg >= _opt.MaxConcurrentPerProxyGroup)
                    throw new InvalidOperationException($"ProxyGroup {proxyGroupId} reached capacity.");
            }

            // idempotent renew nếu chính consumer đang cầm
            var current = await _db.Sessions.FirstOrDefaultAsync(s =>
                s.AccountId == accountId && s.EndedAt == null && s.ExpiresAt > now && s.ConsumerKey == consumerKey, ct);

            if (current is not null)
            {
                current.ExpiresAt = expires;
                await _db.SaveChangesAsync(ct);
                return new AccountLease(current.Id, accountId, proxyGroupId, current.ExpiresAt);
            }

            // không cho 2 session sống cho cùng acc
            var existsLive = await _db.Sessions.AnyAsync(s =>
                s.AccountId == accountId && s.EndedAt == null && s.ExpiresAt > now, ct);
            if (existsLive) throw new InvalidOperationException("Account is in-use");

            var session = new Session
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                ProxyGroupId = proxyGroupId,
                ConsumerKey = consumerKey,
                StartedAt = now.UtcDateTime,
                ExpiresAt = expires.UtcDateTime
            };

            _db.Sessions.Add(session);
            acc.LastUsedAt = now;
            await _db.SaveChangesAsync(ct);

            return new AccountLease(session.Id, accountId, proxyGroupId, expires);
        }

        public async Task<bool> RenewAsync(Guid sessionId, TimeSpan? extendBy = null, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var s = await _db.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.EndedAt == null, ct);
            if (s is null) return false;

            var by = extendBy ?? TimeSpan.FromSeconds(_opt.HeartbeatRenewSeconds);
            var newExp = (s.ExpiresAt != default ? s.ExpiresAt : now).Add(by);
            s.ExpiresAt = newExp;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ReleaseAsync(Guid sessionId, bool checkpoint = false, string? note = null, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var s = await _db.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.EndedAt == null, ct);
            if (s is null) return false;

            s.EndedAt = now.UtcDateTime;
            s.Note = note;

            // cập nhật account
            var acc = await _db.FbAccounts.FirstAsync(a => a.Id == s.AccountId, ct);
            acc.LastUsedAt = now;

            if (checkpoint)
            {
                acc.Status = AccountStatus.Checkpointed;
                acc.CooldownUntil = now.AddHours(_opt.CooldownHoursAfterCheckpoint);
                acc.CheckpointCount = (acc.CheckpointCount ?? 0) + 1;
                acc.LastCheckpoint = now.UtcDateTime;
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
