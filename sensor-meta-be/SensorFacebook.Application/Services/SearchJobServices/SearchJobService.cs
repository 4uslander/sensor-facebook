using Hangfire.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Abstractions;
using SensorFacebook.Shared.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.SearchJobServices
{
    public sealed class SearchJobService : ISearchJobService
    {
        private readonly SensorDbContext _db;
        private readonly IBusPublisher _bus;
        private readonly ILogger<SearchJobService> _log;
        public SearchJobService(SensorDbContext db, IBusPublisher bus, ILogger<SearchJobService> log)
        {
            _db = db; _bus = bus; _log = log;
        }

        public async Task<Guid> RunNowAsync(
            int keywordId,
            string priority,
            int proxyGroupId,
            Guid? accountId,
            CancellationToken ct = default)
        {
            // 1) keyword tồn tại?
            var kwExists = await _db.Keywords.AnyAsync(x => x.Id == keywordId, ct);
            if (!kwExists) throw new ArgumentException("Keyword not found");

            // 2) validate proxyGroupId tồn tại (nếu có table ProxyGroups)
            var pgExists = await _db.ProxyGroups.AnyAsync(x => x.Id == proxyGroupId, ct);
            if (!pgExists) throw new ArgumentException("ProxyGroup not found");

            // 3) nếu Bệ Hạ muốn “cố định account”, validate accountId
            if (accountId is not null)
            {
                var accOk = await _db.FbAccounts.AnyAsync(x => x.Id == accountId && x.ProxyGroupId == proxyGroupId, ct);
                if (!accOk) throw new ArgumentException("Account not found or not in this ProxyGroup");
            }

            // 4) tạo job (Option A: set trước khi publish)
            var scheduledAt = DateTimeOffset.UtcNow;

            var job = new SearchJob
            {
                Id = Guid.NewGuid(),
                KeywordId = keywordId,
                Status = JobStatus.queued,
                Attempts = 0,
                ProxyGroupId = proxyGroupId,
                AccountId = accountId,               // có thể null nếu Worker tự chọn account
                ScheduledAt = scheduledAt.UtcDateTime
            };

            _db.SearchJobs.Add(job);
            await _db.SaveChangesAsync(ct);

            // 5) routing
            var isHigh = string.Equals(priority, "high", StringComparison.OrdinalIgnoreCase);
            var routing = isHigh ? "search.high" : "search.low";
            var prio = isHigh ? "high" : "low";

            // 6) LOG trước khi publish
            _log.LogInformation("Publish SearchJobMsg jobId={JobId} kw={KeywordId} pg={Pg} acc={Acc} routing={Routing}",
                job.Id, keywordId, proxyGroupId, accountId, routing);

            await _bus.PublishAsync(
                routing,
                new SearchJobMsg(
                    JobId: job.Id,
                    KeywordId: keywordId,
                    AccountId: accountId,
                    ProxyGroupId: proxyGroupId,
                    Priority: prio,
                    ScheduledAt: scheduledAt,
                    CorrelationId: null
                ),
                ct
            );

            _log.LogInformation("Published SearchJobMsg jobId={JobId}", job.Id);

            return job.Id;
        }


        public async Task<JobDetailDto?> GetAsync(Guid jobId, CancellationToken ct = default)
        {
            var j = await _db.SearchJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobId, ct);
            if (j is null) return null;
            return new JobDetailDto(
                j.Id, j.KeywordId, j.Status.ToString(), j.Attempts ?? 0, j.ResultCount ?? 0,
                j.ErrorMessage, j.ScheduledAt, j.StartedAt, j.FinishedAt, j.LastErrorAt);
        }

        public async Task<(IReadOnlyList<JobListItemDto> items, int total)> ListAsync(
            string? status, int? keywordId, DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var q = _db.SearchJobs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobStatus>(status, true, out var st))
                q = q.Where(x => x.Status == st);

            if (keywordId is not null)
                q = q.Where(x => x.KeywordId == keywordId.Value);

            if (from is not null) q = q.Where(x => x.ScheduledAt >= from);
            if (to is not null) q = q.Where(x => x.ScheduledAt <= to);

            var total = await q.CountAsync(ct);
            var list = await q.OrderByDescending(x => x.ScheduledAt)
                              .Skip((page - 1) * pageSize).Take(pageSize)
                              .Select(x => new JobListItemDto(
                                  x.Id, x.KeywordId, x.Status.ToString(), x.Attempts ?? 0,
                                  x.ScheduledAt, x.StartedAt, x.FinishedAt))
                              .ToListAsync(ct);
            return (list, total);
        }

        public async Task<bool> RetryAsync(Guid jobId, CancellationToken ct = default)
        {
            var j = await _db.SearchJobs.FirstOrDefaultAsync(x => x.Id == jobId, ct);
            if (j is null) return false;

            // chỉ cho retry khi FAILED
            if (j.Status != JobStatus.failed) return false;

            j.Status = JobStatus.queued;

            var scheduledAt = DateTimeOffset.UtcNow;
            j.ScheduledAt = scheduledAt.UtcDateTime;

            // reset info lỗi (khuyến nghị)
            j.ErrorMessage = null;
            j.LastErrorAt = null;

            await _db.SaveChangesAsync(ct);

            await _bus.PublishAsync(
                "search.low",
                new SearchJobMsg(
                    JobId: j.Id,
                    KeywordId: j.KeywordId ?? 0,
                    AccountId: j.AccountId,
                    ProxyGroupId: j.ProxyGroupId,
                    Priority: "low",
                    ScheduledAt: scheduledAt,
                    CorrelationId: null
                ),
                ct
            );

            return true;
        }

        public async Task<bool> CancelAsync(Guid jobId, CancellationToken ct = default)
        {
            var j = await _db.SearchJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobId, ct);
            if (j is null) return false;

            // Vì enum chỉ có 4 trạng thái nên không có "canceled"
            // Nếu muốn cancel thật sự => phải thêm status canceled vào enum + DB constraint.
            return false;
        }

        public async Task<(IReadOnlyList<JobListItemDto> items, int total)> ListFailedAsync(
            int? keywordId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var q = _db.SearchJobs.AsNoTracking().Where(x => x.Status == JobStatus.failed);

            if (keywordId is not null)
                q = q.Where(x => x.KeywordId == keywordId.Value);

            var total = await q.CountAsync(ct);
            var list = await q.OrderByDescending(x => x.LastErrorAt ?? x.FinishedAt ?? x.ScheduledAt)
                              .Skip((page - 1) * pageSize).Take(pageSize)
                              .Select(x => new JobListItemDto(
                                  x.Id, x.KeywordId, x.Status.ToString(), x.Attempts ?? 0,
                                  x.ScheduledAt, x.StartedAt, x.FinishedAt))
                              .ToListAsync(ct);
            return (list, total);
        }
    }
}
