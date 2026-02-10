namespace RealtimeOutbox.OutboxWorker;

public sealed class OutboxWorkerOptions
{
    public int PollIntervalMs { get; set; } = 500;
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = 20;
}