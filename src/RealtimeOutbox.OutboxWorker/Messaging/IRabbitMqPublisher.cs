namespace RealtimeOutbox.OutboxWorker;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string eventType, string payload, CancellationToken ct);
}