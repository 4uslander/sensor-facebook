using RabbitMQ.Client;
using SensorFacebook.Shared.Abstractions;
using SensorFacebook.Shared.Messaging;
using System.Text.Json;

namespace SensorFacebook.Infrastructure.Messaging
{
    public sealed class RabbitPublisher : IBusPublisher
    {
        private readonly RabbitMQ.Client.IConnection _conn;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public RabbitPublisher(RabbitMQ.Client.IConnection conn) { _conn = conn; }

        public async Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default)
        {
            await using var ch = await _conn.CreateChannelAsync(cancellationToken: ct);

            var body = JsonSerializer.SerializeToUtf8Bytes(payload, _json);

            var props = new BasicProperties
            {
                Persistent = true
            };

            await ch.BasicPublishAsync(
                exchange: Queues.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct
            );
        }
    }
}
