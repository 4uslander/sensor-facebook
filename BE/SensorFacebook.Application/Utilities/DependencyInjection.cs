using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Utilities
{
    public static class DependencyInjection
    {
        /// <summary>
        /// API/Worker/Scheduler gọi method này trong Program.cs
        /// </summary>
        public static IServiceCollection InfrastructureRegister(this IServiceCollection services, IConfiguration cfg)
        {
            // Hiện tại chỉ gom DI của Application
            services.ServiceRegister(cfg);
            return services;
        }
    }
}
