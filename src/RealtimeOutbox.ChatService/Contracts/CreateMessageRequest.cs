namespace RealtimeOutbox.ChatService.Contracts;

public sealed record CreateMessageRequest
(
    Guid TenantId,
    Guid ChannelId,
    Guid SenderUserId,
    string Content
);