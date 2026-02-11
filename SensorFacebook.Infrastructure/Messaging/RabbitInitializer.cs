using RabbitMQ.Client;
using SensorFacebook.Shared.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SensorFacebook.Infrastructure.Messaging
{
    public interface IRabbitInitializer
    {
        Task EnsureTopologyAsync();
    }

    public sealed class RabbitInitializer : IRabbitInitializer
    {
        private readonly RabbitMQ.Client.IConnection _conn;
        public RabbitInitializer(RabbitMQ.Client.IConnection conn) { _conn = conn; }

        public async Task EnsureTopologyAsync()
        {
            
            await using var ch = await _conn.CreateChannelAsync();

            await ch.ExchangeDeclareAsync(Queues.Exchange, ExchangeType.Direct, durable: true);
            await ch.ExchangeDeclareAsync(Queues.DeadLetterExchange, ExchangeType.Direct, durable: true);

            await DeclareQueueAsync(ch, Queues.SearchLow);
            await DeclareQueueAsync(ch, Queues.SearchHigh);
            await DeclareQueueAsync(ch, Queues.ProxyHealth);
            await DeclareQueueAsync(ch, Queues.Notify);

            await ch.QueueDeclareAsync(queue: Queues.Poison, durable: true, exclusive: false, autoDelete: false);

            await ch.QueueBindAsync(Queues.SearchLow, Queues.Exchange, "search.low");
            await ch.QueueBindAsync(Queues.SearchHigh, Queues.Exchange, "search.high");
            await ch.QueueBindAsync(Queues.ProxyHealth, Queues.Exchange, "proxy.health");
            await ch.QueueBindAsync(Queues.Notify, Queues.Exchange, "notify");
            await ch.QueueBindAsync(Queues.Poison, Queues.DeadLetterExchange, "poison");
        }

        private static async Task DeclareQueueAsync(IChannel ch, string name)
        {
            var args = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = Queues.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = "poison",
                ["x-message-ttl"] = 30 * 60 * 1000 
            };

            await ch.QueueDeclareAsync(queue: name, durable: true, exclusive: false, autoDelete: false, arguments: args);
        }
    }
}
