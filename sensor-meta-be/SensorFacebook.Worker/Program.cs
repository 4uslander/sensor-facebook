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

Console.WriteLine($"[BOOT] BaseDir = {AppContext.BaseDirectory}");
Console.WriteLine($"[BOOT] Args = {string.Join(" ", args)}");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // ===== LOGGING =====
        services.AddLogging(o =>
        {
            o.AddSimpleConsole(x =>
            {
                x.TimestampFormat = "HH:mm:ss ";
                x.SingleLine = true;
            });
        });

        // ===== BOOT LOGS (để biết chắc đang chạy đúng config) =====
        var rabbitUri = cfg.GetConnectionString("Rabbit") ?? cfg["Rabbit:Uri"];
        Console.WriteLine($"[BOOT] Env = {ctx.HostingEnvironment.EnvironmentName}");
        Console.WriteLine($"[BOOT] RabbitUri = {rabbitUri}");
        Console.WriteLine($"[BOOT] Queues: low={Queues.SearchLow}, high={Queues.SearchHigh}, health={Queues.ProxyHealth}, notify={Queues.Notify}");

        // ===== DB =====
        services.AddDbContextPool<SensorDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        // ===== RABBIT =====
        services.AddRabbitAndRedis(cfg);

        // ===== HANDLERS =====
        services.AddScoped<IMessageHandler<SearchJobMsg>, SearchJobHandler>();
        services.AddScoped<IMessageHandler<ProxyHealthMsg>, ProxyHealthHandler>();
        services.AddScoped<IMessageHandler<NotifyMsg>, NotifyHandler>();

        // ===== WORKERS (log rõ từng consumer được register) =====
        services.AddSingleton<IHostedService>(sp =>
    new RabbitWorker<SearchJobMsg>(
        sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
        Queues.SearchLow,
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<RabbitWorker<SearchJobMsg>>>()));

        services.AddSingleton<IHostedService>(sp =>
            new RabbitWorker<SearchJobMsg>(
                sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
                Queues.SearchHigh,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<RabbitWorker<SearchJobMsg>>>()));

        services.AddSingleton<IHostedService>(sp =>
            new RabbitWorker<ProxyHealthMsg>(
                sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
                Queues.ProxyHealth,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<RabbitWorker<ProxyHealthMsg>>>()));

        services.AddSingleton<IHostedService>(sp =>
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