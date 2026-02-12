using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SensorFacebook.Worker.Messaging;

public sealed class RabbitWorker<TMsg> : BackgroundService
{
    private readonly IConnection _conn;
    private readonly string _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitWorker<TMsg>> _log;

    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;

    public RabbitWorker(
        IConnection conn,
        string queue,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitWorker<TMsg>> log)
    {
        _conn = conn;
        _queue = queue;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("RabbitWorker<{Type}> starting. Queue={Queue} Endpoint={Endpoint}",
            typeof(TMsg).Name, _queue, _conn.Endpoint?.ToString());

        // v7: tạo channel async
        _channel = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);

        // 1 message / worker để dễ debug
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        _consumer = new AsyncEventingBasicConsumer(_channel);

        _consumer.ReceivedAsync += async (_, ea) =>
        {
            _log.LogInformation("Received. Queue={Queue} DeliveryTag={Tag} Bytes={Len}",
                _queue, ea.DeliveryTag, ea.Body.Length);

            try
            {
                var msg = JsonSerializer.Deserialize<TMsg>(ea.Body.Span);
                if (msg is null) throw new InvalidOperationException("Deserialize returned null");

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMsg>>();

                await handler.HandleAsync(msg, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                _log.LogInformation("Ack OK. Queue={Queue} DeliveryTag={Tag}", _queue, ea.DeliveryTag);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Handle FAILED. Queue={Queue} DeliveryTag={Tag}", _queue, ea.DeliveryTag);

                // requeue=true để thử lại; nếu có DLX/poison thì tuỳ chiến lược đổi thành false
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        var consumerTag = await _channel.BasicConsumeAsync(
            queue: _queue,
            autoAck: false,
            consumer: _consumer,
            cancellationToken: stoppingToken);

        _log.LogInformation("Consuming started. Queue={Queue} ConsumerTag={Tag}", _queue, consumerTag);

        // GIỮ SERVICE SỐNG: nếu thiếu dòng này, consumer sẽ chết và UI sẽ về 0
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync(cancellationToken);
                await _channel.DisposeAsync();
            }
        }
        catch { /* ignore */ }

        await base.StopAsync(cancellationToken);
    }
}
