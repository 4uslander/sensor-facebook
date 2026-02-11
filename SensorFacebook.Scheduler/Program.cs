using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Messaging;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Scheduler.Jobs;

// Host builder
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        services.AddDbContextPool<SensorDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        services.AddRabbitAndRedis(cfg);

        // Hangfire + Redis storage
        services.AddHangfire(hf => hf
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseRedisStorage(cfg.GetSection("Redis")["Configuration"]!));
        services.AddHangfireServer(options => options.Queues = new[] { "scheduler" });

        // schedulers
        services.AddScoped<SearchScheduler>();
        services.AddScoped<ProxyHealthScheduler>();
    });

var app = builder.Build();

// t?o topology RabbitMQ
app.Services.GetRequiredService<IRabbitInitializer>().EnsureTopologyAsync();

// CRON jobs
using (var scope = app.Services.CreateScope())
{
    RecurringJob.AddOrUpdate<SearchScheduler>(
        "kw:rolling", s => s.EnqueueDueKeywords(CancellationToken.None), "*/10 * * * *");
    RecurringJob.AddOrUpdate<ProxyHealthScheduler>(
        "proxy:health", s => s.Enqueue(CancellationToken.None), "*/2 * * * *");
}

await app.RunAsync();
