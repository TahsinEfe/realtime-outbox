namespace RealtimeOutbox.ChatService.Entities;
public sealed class Message
{
    public Guid MessageId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChannelId { get; set; }
     public Guid SenderUserId { get; set; }
     public string Content { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }   
}