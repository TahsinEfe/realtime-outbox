namespace RealtimeOutbox.ChatService.Contracts;

public sealed class OutboxEvent
{
    public Guid OutboxEventId { get; set; }
    public Guid TenantId { get; set; }
    public string Type { get; set; } = null!;
    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }

    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}