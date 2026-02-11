using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Application.Services.LocationServices;
using SensorFacebook.Application.Services.SearchExecutor;
using SensorFacebook.Browser.Search;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Browser
{
    public static class BrowserDi
    {
        public static IServiceCollection AddBrowserPool(this IServiceCollection services, IConfiguration cfg)
        {
            services.Configure<PlaywrightOptions>(cfg.GetSection("Playwright"));
            //services.AddSingleton<IBrowserPool, PlaywrightBrowserPool>();
            services.AddScoped<IBrowserPool, PlaywrightBrowserPool>();
            return services;
        }

        public static IServiceCollection AddSearchExecutor(this IServiceCollection services)
        {
            services.AddScoped<IRadiusNormalizer, RadiusNormalizer>();
            services.AddScoped<ISearchExecutor, FacebookMarketplaceSearchExecutor>();
            services.AddScoped<IKeywordConfigBuilder, KeywordConfigBuilder>();
            return services;
        }
    }
}
