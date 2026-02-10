using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RealtimeOutbox.RealtimeGateway.Workers;

public sealed class RabbitMqConsumer(
    ILogger<RabbitMqConsumer> logger,
    IHubContext<ChatHub> hub,
    IConfiguration config
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RabbitMqConsumer starting...");

        var uri = config.GetValue<string>("RabbitMq:Uri") 
                  ?? "amqp://outbox:outbox@localhost:5672/";

        var queue = config.GetValue<string>("RabbitMq:Queue") 
                    ?? "outbox_events_queue";

        var factory = new ConnectionFactory
        {
            Uri = new Uri(uri),
            AutomaticRecoveryEnabled = true
        };

        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.BasicQosAsync(0, 20, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        logger.LogInformation("Message received: {Json}", json);

        await hub.Clients.All.SendAsync("outbox.test", json, cancellationToken: stoppingToken);

        var eventType = ea.BasicProperties?.Type;

        if (eventType == "ChatMessageCreated")
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tenantId", out var t) ||
                !doc.RootElement.TryGetProperty("channelId", out var c))
            {
                logger.LogWarning("Missing tenantId/channelId in payload. Json={Json}", json);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                return;
            }

            var tenantId = t.GetString();
            var channelId = c.GetString();
            var group = $"{tenantId}:{channelId}";

            await hub.Clients.Group(group)
                .SendAsync("chat.message.created", json, cancellationToken: stoppingToken);
        }

        await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Consumer failed to process message");
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
        }
    };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        logger.LogInformation("RabbitMqConsumer connected to queue {Queue}", queue);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        
    }
}