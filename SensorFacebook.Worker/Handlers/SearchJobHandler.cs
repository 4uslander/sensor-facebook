using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Application.Services.ListingServices;
using SensorFacebook.Application.Services.SearchExecutor;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Messaging;
using SensorFacebook.Worker.Messaging;

namespace SensorFacebook.Worker.Handlers
{
    public sealed class SearchJobHandler : IMessageHandler<SearchJobMsg>
    {
        private readonly SensorDbContext _db;
        private readonly IBrowserPool _pool;
        private readonly IAccountSelector _selector;
        private readonly IKeywordConfigBuilder _cfgBuilder;
        private readonly ISearchExecutor _executor;
        private readonly ILogger<SearchJobHandler> _log;
        private readonly IListingUpsertService _listingUpsert;

        public SearchJobHandler(
        SensorDbContext db,
        IBrowserPool pool,
        IAccountSelector selector,
        IKeywordConfigBuilder cfgBuilder,
        ISearchExecutor executor,
        IListingUpsertService listingUpsert,
        ILogger<SearchJobHandler> log)
        {
            _db = db; _pool = pool; _selector = selector;
            _cfgBuilder = cfgBuilder; _executor = executor;
            _listingUpsert = listingUpsert;
            _log = log;
        }

        public async Task HandleAsync(SearchJobMsg msg, CancellationToken ct)
        {
            var job = await _db.SearchJobs.FirstOrDefaultAsync(x => x.Id == msg.JobId, ct);
            if (job is null) return;

            // Chỉ chạy khi queued
            if (job.Status != JobStatus.queued) return;

            job.Status = JobStatus.running;
            job.StartedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            AccountLease? lease = null;
            PeriodicTimer? hbTimer = null;
            CancellationTokenSource? hbCts = null;

            try
            {
                if (job.KeywordId is null) throw new InvalidOperationException("Job missing KeywordId");
                var cfg = await _cfgBuilder.BuildAsync(job.KeywordId.Value, ct);

                int? chosenPg = msg.ProxyGroupId;
                if (chosenPg is null)
                    throw new InvalidOperationException("No proxy group resolved for this job/keyword.");

                lease = await _selector.AcquireAsync(
                    requiredProxyGroupId: chosenPg.Value,
                    priority: LeasePriority.High,
                    consumerKey: "worker.search",
                    ttl: TimeSpan.FromMinutes(15),
                    ct: ct);

                await using var brLease = await _pool.AcquireAsync(lease.AccountId, lease.ProxyGroupId, ct);
                var page = (IPage)await brLease.NewPageAsync(ct);

                hbTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                hbCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (await hbTimer.WaitForNextTickAsync(hbCts.Token))
                            await _selector.RenewAsync(lease.SessionId, null, hbCts.Token);
                    }
                    catch { }
                }, hbCts.Token);

                await page.GotoAsync("https://www.facebook.com/marketplace", new()
                {
                    Timeout = 45000,
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                _log.LogInformation("SearchJob {JobId} start, keyword={KeywordId}, pg={Pg}, acc={Acc}",
                    msg.JobId, job.KeywordId, lease.ProxyGroupId, lease.AccountId);

                var result = await _executor.ExecuteAsync(page, cfg, ct);

                _log.LogInformation("SearchJob {JobId} executor done, parsed items={Count}",
                    msg.JobId, result.Items.Count);

                var now = DateTimeOffset.UtcNow;
                var upserted = 0;
                foreach (var it in result.Items)
                {
                    upserted += await _listingUpsert.UpsertAsync(
                        it: it,
                        keywordId: job.KeywordId,
                        accountId: lease.AccountId,
                        proxyGroupId: lease.ProxyGroupId,
                        now: now,
                        ct: ct);
                }

                job.ResultCount = upserted;
                job.Status = JobStatus.done;
                job.FinishedAt = DateTime.UtcNow;

                await page.CloseAsync();
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Search job {Id} failed", msg.JobId);

                job.Attempts = (job.Attempts ?? 0) + 1;
                job.ErrorMessage = ex.Message.Length > 800 ? ex.Message[..800] : ex.Message;
                job.LastErrorAt = DateTime.UtcNow;
                job.FinishedAt = DateTime.UtcNow;

                // 4-status policy: <=4 lần thì quay lại queued, >=5 thì failed
                if (job.Attempts >= 5)
                {
                    job.Status = JobStatus.failed;
                }
                else
                {
                    job.Status = JobStatus.queued;

                    // backoff để tránh loop ngay lập tức (nếu bạn có scheduler)
                    var delay = job.Attempts switch
                    {
                        1 => TimeSpan.FromMinutes(1),
                        2 => TimeSpan.FromMinutes(5),
                        3 => TimeSpan.FromMinutes(15),
                        _ => TimeSpan.FromMinutes(30)
                    };
                    job.ScheduledAt = DateTime.UtcNow.Add(delay);
                }

                await _db.SaveChangesAsync(ct);
            }
            finally
            {
                try { hbCts?.Cancel(); hbTimer?.Dispose(); } catch { }

                if (lease is not null)
                {
                    try { await _selector.ReleaseAsync(lease.SessionId, checkpoint: false, note: "search-done", ct); }
                    catch (Exception rex) { _log.LogWarning(rex, "Release session failed: {Sid}", lease.SessionId); }
                }
            }
        }
    }
}