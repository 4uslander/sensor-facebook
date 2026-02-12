using Microsoft.EntityFrameworkCore;          
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Abstractions;
using SensorFacebook.Shared.Messaging;
using System;

namespace SensorFacebook.Scheduler.Jobs
{
    public sealed class SearchScheduler
    {
        private readonly SensorDbContext _db;
        private readonly IBusPublisher _bus;

        public SearchScheduler(SensorDbContext db, IBusPublisher bus)
        { _db = db; _bus = bus; }

        public async Task EnqueueDueKeywords(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var nowUnix = new DateTimeOffset(now).ToUnixTimeSeconds(); 

            var due = await _db.Keywords
                .Where(k => (k.Active ?? true) && k.NextRun <= now)
                .Select(k => k.Id)                 
                .ToListAsync(ct);                  

            foreach (var keywordId in due)
            {
                var job = new SearchJob
                {
                    Id = Guid.NewGuid(),
                    KeywordId = keywordId,
                    Status = JobStatus.queued,
                    ScheduledAt = now
                };

                _db.SearchJobs.Add(job);

                var bucket = nowUnix / 180; // 3 phút
                var msg = new SearchJobMsg(
                    job.Id,
                    keywordId,
                    null,
                    null,
                    "low",
                    now,
                    $"kw:{keywordId}:{bucket}"
                );

                await _bus.PublishAsync("search.low", msg, ct);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
