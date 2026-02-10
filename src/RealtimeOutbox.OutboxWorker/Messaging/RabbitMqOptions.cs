namespace RealtimeOutbox.OutboxWorker;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";

    public string Exchange { get; set; } = "chat.events";
    public string RoutingKey { get; set; } = "chat.events";

    public bool Durable { get; set; } = true;
}