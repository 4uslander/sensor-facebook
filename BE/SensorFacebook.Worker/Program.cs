using Microsoft.EntityFrameworkCore;
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

        services.AddDbContextPool<SensorDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        services.AddRabbitAndRedis(cfg);

        services.AddScoped<IMessageHandler<SearchJobMsg>, SearchJobHandler>();
        services.AddScoped<IMessageHandler<ProxyHealthMsg>, ProxyHealthHandler>();
        services.AddScoped<IMessageHandler<NotifyMsg>, NotifyHandler>();

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
