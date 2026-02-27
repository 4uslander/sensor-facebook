using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Application.Services.AuthServices;
using SensorFacebook.Application.Services.Cache;
using SensorFacebook.Application.Services.CategoryServices;
using SensorFacebook.Application.Services.KeywordServices;
using SensorFacebook.Application.Services.ListingServices;
using SensorFacebook.Application.Services.LocationServices;
using SensorFacebook.Application.Services.PasswordServices;
using SensorFacebook.Application.Services.ProxyGroups;
using SensorFacebook.Application.Services.SearchExecutor;
using SensorFacebook.Application.Services.SearchJobServices;
using SensorFacebook.Application.Services.TokenServices;
using SensorFacebook.Application.Services.UserServices;
using SensorFacebook.Infrastructure.Messaging;
using SensorFacebook.Shared.Abstractions;

namespace SensorFacebook.Application.Utilities
{
    public static class ModuleRegister
    {
        /// <summary>
        /// Đăng ký toàn bộ Application Services (không đụng DbContext/Infrastructure).
        /// </summary>
        public static IServiceCollection ServiceRegister(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IKeywordService, KeywordService>();
            services.AddScoped<IRadiusNormalizer, RadiusNormalizer>();
            services.AddScoped<IKeywordImportExportService, KeywordImportExportService>();

            services.AddScoped<IProxyGroupService, ProxyGroupService>();
            services.AddScoped<IProxyHealthService, ProxyHealthService>();

            // Cache abstractions
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ICacheBustService, CacheBustService>();

            // ===================== RABBITMQ (use URI, not guest/localhost) =====================
            services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
            {
                var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RabbitConn(Api)");

                // ưu tiên ConnectionStrings:Rabbit, fallback Rabbit:Uri
                var uri = cfg.GetConnectionString("Rabbit") ?? cfg["Rabbit:Uri"];
                log.LogInformation("API Rabbit URI = {Uri}", uri);

                if (string.IsNullOrWhiteSpace(uri))
                    throw new InvalidOperationException(
                        "Missing Rabbit connection string. Add ConnectionStrings:Rabbit or Rabbit:Uri.");

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(uri),
                    // optional: giúp dễ thấy connection trong UI
                    ClientProvidedName = "sensor-facebook-api"
                };

                var conn = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                log.LogInformation("API Rabbit CONNECTED. Endpoint={Endpoint}", conn.Endpoint?.ToString());

                return conn;
            });

            services.AddSingleton<IRabbitInitializer, RabbitInitializer>();
            services.AddSingleton<IBusPublisher, RabbitPublisher>();
            // =============================================================================

            services.AddScoped<ISearchJobService, SearchJobService>();
            services.AddScoped<IListingQueryService, ListingQueryService>();

            services.AddScoped<IAccountService, AccountService>();
            services.AddSingleton<ICookieCryptoService, CookieCryptoService>();
            services.AddScoped<IAccountSelector, AccountSelectorService>();
            services.AddScoped<IKeywordConfigBuilder, KeywordConfigBuilder>();
            services.AddScoped<IListingUpsertService, ListingUpsertService>();

            return services;
        }
    }
}