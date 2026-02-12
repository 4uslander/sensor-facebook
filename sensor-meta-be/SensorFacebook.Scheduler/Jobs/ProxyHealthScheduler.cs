using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Abstractions;
using SensorFacebook.Shared.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Scheduler.Jobs
{
    public sealed class ProxyHealthScheduler
    {
        private readonly SensorDbContext _db;
        private readonly IBusPublisher _bus;

        public ProxyHealthScheduler(SensorDbContext db, IBusPublisher bus)
        { _db = db; _bus = bus; }

        public async Task Enqueue(CancellationToken ct = default)
        {
            var ids = await _db.ProxyGroups.Select(x => x.Id).ToListAsync(ct);
            var now = DateTimeOffset.UtcNow;
            foreach (var id in ids)
                await _bus.PublishAsync("proxy.health", new ProxyHealthMsg(id, now), ct);
        }
    }
}
