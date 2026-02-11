using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using SensorFacebook.Shared.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

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
            services.AddScoped<IKeywordService, KeywordService>(); // (KeywordService mới đã bỏ cache)
            services.AddScoped<IRadiusNormalizer, RadiusNormalizer>();
            services.AddScoped<IKeywordImportExportService, KeywordImportExportService>();

            services.AddScoped<IProxyGroupService, ProxyGroupService>();
            services.AddScoped<IProxyHealthService, ProxyHealthService>();

            // Cache abstractions (Category còn dùng)
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ICacheBustService, CacheBustService>();

            // ❌ BỎ Redis multiplexer ở đây (để Program.cs lo)
            // services.AddSingleton<IConnectionMultiplexer>(...)

            // RabbitMQ connection
            services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
            {
                var o = new RabbitMqOptions();
                cfg.GetSection("RabbitMQ").Bind(o);

                var factory = new ConnectionFactory
                {
                    HostName = o.HostName,
                    Port = o.Port,
                    UserName = o.UserName,
                    Password = o.Password,
                    VirtualHost = o.VirtualHost
                };

                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton<IRabbitInitializer, RabbitInitializer>();
            services.AddSingleton<IBusPublisher, RabbitPublisher>();

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
