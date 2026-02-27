using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SensorFacebook.Shared.Abstractions;
using SensorFacebook.Shared.Messaging;
using System.Text.Json;

namespace SensorFacebook.Infrastructure.Messaging
{
    public sealed class RabbitPublisher : IBusPublisher
    {
        private readonly RabbitMQ.Client.IConnection _conn;
        private readonly ILogger<RabbitPublisher> _log;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public RabbitPublisher(RabbitMQ.Client.IConnection conn, ILogger<RabbitPublisher> log)
        {
            _conn = conn;
            _log = log;
        }

        public async Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default)
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));

            // ✅ chặn SearchJobMsg rỗng (case bạn đang gặp)
            if (payload is SearchJobMsg sj)
            {
                if (sj.JobId == Guid.Empty) throw new InvalidOperationException("Refuse publish: SearchJobMsg.JobId is empty");
                if (sj.KeywordId <= 0) throw new InvalidOperationException("Refuse publish: SearchJobMsg.KeywordId invalid");
                if (string.IsNullOrWhiteSpace(sj.Priority)) throw new InvalidOperationException("Refuse publish: SearchJobMsg.Priority empty");
            }

            await using var ch = await _conn.CreateChannelAsync(cancellationToken: ct);

            var body = JsonSerializer.SerializeToUtf8Bytes(payload, _json);
            if (body.Length == 0) throw new InvalidOperationException("Serialized body is empty");

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                MessageId = Guid.NewGuid().ToString("N"),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            _log.LogInformation("Publish exchange={Ex} rk={RK} type={Type} bytes={Bytes}",
                Queues.Exchange, routingKey, typeof(T).Name, body.Length);

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