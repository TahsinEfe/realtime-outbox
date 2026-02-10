using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RealtimeOutbox.OutboxWorker.Messaging;

public sealed class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_channel is not null && _connection is not null) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_channel is not null && _connection is not null) return;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Pass
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: ExchangeType.Topic,
                durable: _options.Durable,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync(string eventType, string payload, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct);

        var routingKey = string.IsNullOrWhiteSpace(eventType) ? _options.RoutingKey : eventType;
        var bodyBytes = Encoding.UTF8.GetBytes(payload);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = eventType
        };


        await _channelLock.WaitAsync(ct);
        try
        {
            await _channel!.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: bodyBytes,
                cancellationToken: ct);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is not null)
                await _channel.CloseAsync();
        }
        catch {  }

        try
        {
            if (_connection is not null)
                await _connection.CloseAsync();
        }
        catch {  }

        _channel?.Dispose();
        _connection?.Dispose();

        _initLock.Dispose();
        _channelLock.Dispose();
    }
}
