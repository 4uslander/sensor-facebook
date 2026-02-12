using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Application.Services.ListingServices;
using SensorFacebook.Browser;
using SensorFacebook.Infrastructure.Messaging;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Messaging;
using SensorFacebook.Worker.Handlers;
using SensorFacebook.Worker.Hosted;
using SensorFacebook.Worker.Messaging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // ===== LẤY CONFIG RABBIT =====
        var rabbitHost = cfg["Rabbit:Host"];
        var rabbitVhost = cfg["Rabbit:VHost"];
        var rabbitUser = cfg["Rabbit:User"];

        // ===== LOGGING =====
        services.AddLogging(o =>
        {
            o.AddSimpleConsole(x =>
            {
                x.TimestampFormat = "HH:mm:ss ";
                x.SingleLine = true;
            });
        });

        services.PostConfigure<LoggerFilterOptions>(o =>
        {
            // nếu muốn filter log thì cấu hình ở đây
            // ví dụ:
            // o.MinLevel = LogLevel.Information;
        });

        // ===== STARTUP LOG HOSTED SERVICE =====
        services.AddSingleton<IHostedService>(_ =>
            new StartupLogHostedService(rabbitHost, rabbitVhost, rabbitUser));

        // ===== DB =====
        services.AddDbContextPool<SensorDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        // ===== RABBIT + REDIS =====
        services.AddRabbitAndRedis(cfg);

        // ===== HANDLERS =====
        services.AddScoped<IMessageHandler<SearchJobMsg>, SearchJobHandler>();
        services.AddScoped<IMessageHandler<ProxyHealthMsg>, ProxyHealthHandler>();
        services.AddScoped<IMessageHandler<NotifyMsg>, NotifyHandler>();

        // ===== WORKERS =====
        services.AddHostedService(sp =>
            new RabbitWorker<SearchJobMsg>(
                sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
                Queues.SearchLow,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<RabbitWorker<SearchJobMsg>>>()));

        services.AddHostedService(sp =>
            new RabbitWorker<SearchJobMsg>(
                sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
                Queues.SearchHigh,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<RabbitWorker<SearchJobMsg>>>()));

        services.AddHostedService(sp =>
            new RabbitWorker<ProxyHealthMsg>(
                sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
                Queues.ProxyHealth,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<RabbitWorker<ProxyHealthMsg>>>()));

        services.AddHostedService(sp =>
            new RabbitWorker<NotifyMsg>(
                sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
                Queues.Notify,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<RabbitWorker<NotifyMsg>>>()));

        // ===== OTHER SERVICES =====
        services.AddCookieCrypto(cfg);
        services.AddBrowserPool(cfg);
        services.AddScoped<IAccountSelector, AccountSelectorService>();
        services.AddScoped<IListingUpsertService, ListingUpsertService>();
        services.Configure<AccountSelectorOptions>(cfg.GetSection("AccountSelector"));
        services.AddHostedService<CleanupExpiredSessionsHostedService>();

        services.AddSearchExecutor();
    })
    .Build();

// đảm bảo topology
await host.Services.GetRequiredService<IRabbitInitializer>().EnsureTopologyAsync();

await host.RunAsync();
