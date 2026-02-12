using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace SensorFacebook.Infrastructure.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitAndRedis(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddSingleton<RabbitMQ.Client.IConnection>(_ =>
            {
                var uri = cfg.GetConnectionString("Rabbit") ?? cfg["Rabbit:Uri"];
                if (string.IsNullOrWhiteSpace(uri))
                    throw new InvalidOperationException("Missing Rabbit connection string. Add ConnectionStrings:Rabbit or Rabbit:Uri.");

                var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    Uri = new Uri(uri)
                };

                // v7: chỉ có async
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton<IRabbitInitializer, RabbitInitializer>();
            return services;
        }
    }
}
