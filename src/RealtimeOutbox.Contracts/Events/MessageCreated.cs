namespace RealtimeOutbox.Contracts.Events;


public sealed record MessageCreated(
    Guid TenantId,
    Guid ChannelId,
    Guid MessageId,
    Guid SenderUserId,
    string Content,
    DateTimeOffset CreatedAtUtc
);
