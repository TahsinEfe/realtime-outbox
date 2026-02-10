using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.OutboxWorker.Data;
using System.Text;
using RabbitMQ.Client;

namespace RealtimeOutbox.OutboxWorker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceProvider sp,
    IConfiguration config
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rmUri = config.GetValue<string>("RabbitMq:Uri")
                     ?? "amqp://outbox:outbox@localhost:5672/";


        var factory = new ConnectionFactory { Uri = new Uri(rmUri) };

        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: "outbox_events",
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.QueueDeclareAsync(
            queue: "outbox_events_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.QueueBindAsync(
            queue: "outbox_events_queue",
            exchange: "outbox_events",
            routingKey: "",
            arguments: null,
            cancellationToken: stoppingToken
        );

        

        logger.LogInformation(
            "Worker started. Publishing outbox events to RabbitMQ {Uri}",
            rmUri
        );

        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = sp.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

                var batch = await dbContext.OutboxEvents
                    .Where(e => e.SentAtUtc == null)
                    .OrderBy(e => e.OccurredAtUtc)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var e in batch)
                {
                    try
                    {
                      var body = Encoding.UTF8.GetBytes(e.Payload);

                      var props = new BasicProperties
                        {
                          Persistent = true,
                          Type = e.Type,
                          MessageId = e.Id.ToString(),
                            Timestamp = new AmqpTimestamp(e.OccurredAtUtc.ToUnixTimeSeconds())  
                        };

                        await channel.BasicPublishAsync(
                            exchange: "outbox_events",
                            routingKey: e.Type, 
                            mandatory: false,
                            basicProperties: props,
                            body: body,
                            cancellationToken: stoppingToken
                        );

                        e.SentAtUtc = DateTimeOffset.UtcNow;
                        e.LastError = null;

                        logger.LogInformation("Published outbox event {EventId} of type {EventType}", e.Id, e.Type);
                    }
                    catch (Exception ex)
                    {
                        e.AttemptCount++;
                        e.LastError = ex.Message;

                        logger.LogError(ex, "Failed to publish outbox event {EventId} of type {EventType}", e.Id, e.Type);
                    }
                }

                 if (batch.Count > 0)
                    await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox poll loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
