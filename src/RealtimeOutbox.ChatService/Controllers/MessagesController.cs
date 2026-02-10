using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.ChatService.Contracts;
using RealtimeOutbox.ChatService.Data;
using RealtimeOutbox.ChatService.Entities;
using RealtimeOutbox.Contracts.Events;

namespace RealtimeOutbox.ChatService.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessageController : ControllerBase
{
    private readonly ChatDbContext _db;

    public MessageController(ChatDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMessageRequest rq, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rq.Content))
            return BadRequest("Content is required.");

        if (rq.Content.Length > 1000)
            return BadRequest("Content must be less than 1000 characters.");

        var now = DateTimeOffset.UtcNow;

        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            TenantId = rq.TenantId,
            ChannelId = rq.ChannelId,
            SenderUserId = rq.SenderUserId,
            Content = rq.Content.Trim(),
            CreatedAtUtc = now
        };

        //  Integration event 
        var evt = new MessageCreated(
            TenantId: message.TenantId,
            ChannelId: message.ChannelId,
            MessageId: message.MessageId,
            SenderUserId: message.SenderUserId,
            Content: message.Content,
            CreatedAtUtc: message.CreatedAtUtc
        );

        var outbox = new OutboxEvent
        {
            OutboxEventId = Guid.NewGuid(),
            TenantId = message.TenantId,
            Type = "message.created",
            PayloadJson = JsonSerializer.Serialize(evt),
            OccurredAtUtc = now,
            SentAtUtc = null,
            AttemptCount = 0,
            LastError = null
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Messages.Add(message);
        _db.OutboxEvents.Add(outbox);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new
        {
            message.MessageId,
            message.TenantId,
            message.ChannelId,
            message.SenderUserId,
            message.Content,
            message.CreatedAtUtc
        });
    }

    [HttpGet("outbox/pending-count")]
    public async Task<IActionResult> PendingOutboxCount(CancellationToken ct)
    {
        var count = await _db.OutboxEvents.CountAsync(x => x.SentAtUtc == null, ct);
        return Ok(new { pending = count });
    }
}
