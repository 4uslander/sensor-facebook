using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SensorFacebook.Infrastructure.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitAndRedis(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
            {
                var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RabbitConn");
                var uri = cfg.GetConnectionString("Rabbit") ?? cfg["Rabbit:Uri"];

                log.LogInformation("Rabbit URI = {Uri}", uri);

                if (string.IsNullOrWhiteSpace(uri))
                    throw new InvalidOperationException("Missing Rabbit connection string...");

                var factory = new ConnectionFactory { Uri = new Uri(uri) };
                var conn = factory.CreateConnectionAsync().GetAwaiter().GetResult();

                log.LogInformation("Rabbit CONNECTED. Endpoint={Endpoint}", conn.Endpoint?.ToString());
                return conn;
            });

            services.AddSingleton<IRabbitInitializer, RabbitInitializer>();
            return services;
        }
    }
}
