using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Application.Services.ListingServices;
using SensorFacebook.Application.Services.SearchExecutor;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
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
            _db = db;
            _pool = pool;
            _selector = selector;
            _cfgBuilder = cfgBuilder;
            _executor = executor;
            _listingUpsert = listingUpsert;
            _log = log;
        }

        public async Task HandleAsync(SearchJobMsg msg, CancellationToken ct)
        {
            _log.LogInformation("Handle SearchJobMsg: jobId={JobId} kw={KeywordId} pg={Pg} acc={Acc} prio={Prio}",
                msg.JobId, msg.KeywordId, msg.ProxyGroupId, msg.AccountId, msg.Priority);

            var job = await _db.SearchJobs.FirstOrDefaultAsync(x => x.Id == msg.JobId, ct);

            if (job is null)
            {
                var conn = _db.Database.GetDbConnection();
                _log.LogWarning("Job NOT FOUND: jobId={JobId}. WorkerDb={Db} DataSource={Ds}",
                    msg.JobId, conn.Database, conn.DataSource);
                return;
            }

            if (job.Status != JobStatus.queued)
            {
                _log.LogInformation("Job SKIP: jobId={JobId} status={Status} attempts={Attempts}",
                    job.Id, job.Status, job.Attempts);
                return;
            }

            // Option A: bắt buộc có ProxyGroupId/AccountId từ producer (msg hoặc job)
            var proxyGroupId = msg.ProxyGroupId ?? job.ProxyGroupId;
            if (proxyGroupId is null)
            {
                await FailAndSaveAsync(job, "Missing ProxyGroupId (Option A requires it).", ct);
                return;
            }

            var fixedAccountId = msg.AccountId ?? job.AccountId;
            if (fixedAccountId is null)
            {
                await FailAndSaveAsync(job, "Missing AccountId (Option A requires it).", ct);
                return;
            }

            // chuyển trạng thái running
            job.Status = JobStatus.running;
            job.StartedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            AccountLease? lease = null;
            PeriodicTimer? hbTimer = null;
            CancellationTokenSource? hbCts = null;

            try
            {
                if (job.KeywordId is null)
                    throw new InvalidOperationException("Job missing KeywordId");

                var cfg = await _cfgBuilder.BuildAsync(job.KeywordId.Value, ct);

                // Option A: mượn đúng account cố định
                lease = await _selector.AcquireByAccountAsync(
                    accountId: fixedAccountId.Value,
                    proxyGroupId: proxyGroupId.Value,
                    consumerKey: $"worker.search:{msg.Priority}",
                    ttl: TimeSpan.FromMinutes(15),
                    ct: ct);

                // Acquire browser context theo account + proxy
                await using var brLease = await _pool.AcquireAsync(lease.AccountId, lease.ProxyGroupId, ct);

                // NOTE (fix): NewPageAsync returns object -> cast to IPage.
                // Cannot use 'await using' with IPage because the provided IPage signature
                // does not implement IAsyncDisposable. Manage async close explicitly.
                var pageObj = await brLease.NewPageAsync(ct);
                var page = (IPage)pageObj;

                try
                {
                    // heartbeat renew session
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

                    _log.LogInformation("SearchJob START: jobId={JobId}, keyword={KeywordId}, pg={Pg}, acc={Acc}",
                        msg.JobId, job.KeywordId, lease.ProxyGroupId, lease.AccountId);

                    await page.GotoAsync("https://www.facebook.com/marketplace", new PageGotoOptions
                    {
                        Timeout = 45000,
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var result = await _executor.ExecuteAsync(page, cfg, ct);

                    _log.LogInformation("SearchJob DONE EXECUTOR: jobId={JobId}, parsedItems={Count}",
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

                    await _db.SaveChangesAsync(ct);

                    _log.LogInformation("SearchJob FINISH: jobId={JobId}, upserted={Upserted}", msg.JobId, upserted);
                }
                finally
                {
                    // Ensure the page is closed asynchronously. Swallow any errors from close.
                    try { await page.CloseAsync(); } catch { }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Search job failed: jobId={JobId}", msg.JobId);

                job.Attempts = (job.Attempts ?? 0) + 1;
                job.ErrorMessage = ex.Message.Length > 800 ? ex.Message[..800] : ex.Message;
                job.LastErrorAt = DateTime.UtcNow;
                job.FinishedAt = DateTime.UtcNow;

                if (job.Attempts >= 5)
                {
                    job.Status = JobStatus.failed;
                }
                else
                {
                    job.Status = JobStatus.queued;

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
                    try
                    {
                        await _selector.ReleaseAsync(lease.SessionId, checkpoint: false, note: "search-done", ct);
                    }
                    catch (Exception rex)
                    {
                        _log.LogWarning(rex, "Release session failed: sid={Sid}", lease.SessionId);
                    }
                }
            }
        }

        private async Task FailAndSaveAsync(SearchJob job, string reason, CancellationToken ct)
        {
            _log.LogError("SearchJob INVALID: jobId={JobId}. {Reason}", job.Id, reason);

            job.Attempts = (job.Attempts ?? 0) + 1;
            job.ErrorMessage = reason.Length > 800 ? reason[..800] : reason;
            job.LastErrorAt = DateTime.UtcNow;
            job.FinishedAt = DateTime.UtcNow;
            job.Status = JobStatus.failed;

            await _db.SaveChangesAsync(ct);
        }
    }
}