using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SensorFacebook.Shared.Messaging;
using System.Text;
using System.Text.Json;

namespace SensorFacebook.Worker.Messaging;

public sealed class RabbitWorker<TMsg> : BackgroundService
{
    private readonly IConnection _conn;
    private readonly string _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitWorker<TMsg>> _log;

    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

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

        _channel = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);
        _log.LogInformation("Channel created. Queue={Queue}", _queue);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        _consumer = new AsyncEventingBasicConsumer(_channel);

        _consumer.ReceivedAsync += async (_, ea) =>
        {
            _log.LogInformation("Received. Queue={Queue} DeliveryTag={Tag} Bytes={Len}",
                _queue, ea.DeliveryTag, ea.Body.Length);

            // ✅ RAW payload log (giới hạn để tránh log quá dài)
            var raw = Encoding.UTF8.GetString(ea.Body.Span);
            _log.LogInformation("RAW payload ({Bytes}) queue={Queue}: {Raw}",
                ea.Body.Length, _queue, raw.Length > 500 ? raw[..500] : raw);

            try
            {
                // ✅ deserialize với options đồng nhất publisher
                var msg = JsonSerializer.Deserialize<TMsg>(ea.Body.Span, _json);
                if (msg is null) throw new InvalidOperationException("Deserialize returned null");

                // ✅ log jobId sau deserialize (nếu là SearchJobMsg)
                if (msg is SearchJobMsg sj)
                {
                    _log.LogInformation("DESERIALIZED SearchJobMsg: jobId={JobId} keywordId={KeywordId} pg={Pg} acc={Acc} prio={Prio}",
                        sj.JobId, sj.KeywordId, sj.ProxyGroupId, sj.AccountId, sj.Priority);

                    if (sj.JobId == Guid.Empty)
                    {
                        _log.LogError("INVALID SearchJobMsg received (empty JobId). Drop message.");
                        await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        return;
                    }
                }

                // ✅ dùng async scope để DI dispose đúng IAsyncDisposable
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMsg>>();
                    await handler.HandleAsync(msg, stoppingToken);
                } // ✅ DisposeAsync xảy ra ở đây. Nếu fail sẽ vào catch.

                // ✅ Ack sau khi handler + DisposeAsync đều OK
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                _log.LogInformation("Ack OK. Queue={Queue} DeliveryTag={Tag}", _queue, ea.DeliveryTag);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Handle FAILED. Queue={Queue} DeliveryTag={Tag}", _queue, ea.DeliveryTag);

                // requeue=true để thử lại (hoặc false nếu bạn muốn đẩy DLX/poison)
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };
        _log.LogInformation("SUBSCRIBE queue={Queue}", _queue);
        var tag = await _channel.BasicConsumeAsync(_queue, autoAck: false, _consumer, stoppingToken);
        _log.LogInformation("Consuming started. Queue={Queue} ConsumerTag={Tag}", _queue, tag);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
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
        catch { }

        await base.StopAsync(cancellationToken);
    }
}