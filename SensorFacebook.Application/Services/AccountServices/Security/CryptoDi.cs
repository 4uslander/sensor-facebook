using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Security
{
    public static class CryptoDi
    {
        public static IServiceCollection AddCookieCrypto(this IServiceCollection services, IConfiguration cfg)
        {
            // Nếu service cần key/opts thì bind ở đây
            // services.Configure<CryptoOptions>(cfg.GetSection("Crypto"));

            services.AddSingleton<ICookieCryptoService, CookieCryptoService>();
            return services;
        }
    }
}
