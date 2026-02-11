using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace SensorFacebook.Worker.Messaging
{
    public sealed class RabbitWorker<T> : BackgroundService
    {
        private readonly IConnection _conn;
        private readonly string _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitWorker<T>> _log;

        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private IChannel? _ch;

        public RabbitWorker(
            IConnection conn,
            string queue,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitWorker<T>> log)
        {
            _conn = conn;
            _queue = queue;
            _scopeFactory = scopeFactory;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ch = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);

            // QoS: bạn đang để prefetchCount = 2, OK
            await _ch.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 2,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_ch);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                // Tạo scope riêng cho từng message (đúng DI lifetime)
                using var scope = _scopeFactory.CreateScope();

                try
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();

                    var msg = JsonSerializer.Deserialize<T>(ea.Body.Span, _json);
                    if (msg is null)
                        throw new InvalidOperationException($"Cannot deserialize message to {typeof(T).Name}");

                    // Dùng stoppingToken là ok; nếu muốn timeout per-message thì bạn tạo CTS riêng.
                    await handler.HandleAsync(msg, stoppingToken);

                    await _ch.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Consume failed. queue={Queue}, type={Type}, deliveryTag={Tag}",
                        _queue, typeof(T).Name, ea.DeliveryTag);

                    // Nack không requeue -> đi DLX/poison theo topology bạn đã khai báo
                    try
                    {
                        await _ch.BasicNackAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            requeue: false,
                            cancellationToken: stoppingToken);
                    }
                    catch (Exception nackEx)
                    {
                        _log.LogError(nackEx, "BasicNack failed. queue={Queue}, deliveryTag={Tag}",
                            _queue, ea.DeliveryTag);
                    }
                }
            };

            await _ch.BasicConsumeAsync(
                queue: _queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            // giữ worker sống
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_ch is not null)
                await _ch.DisposeAsync();

            await base.StopAsync(cancellationToken);
        }
    }
}
