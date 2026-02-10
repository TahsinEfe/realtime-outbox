using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealtimeOutbox.OutboxWorker.Entities;

public sealed class OutboxEvent
{
    [Key]
    [Column("OutboxEventId")]
    public Guid Id { get; set; }

    [Column("TenantId")]
    public Guid TenantId { get; set; }

    [Column("Type")]
    public string Type { get; set; } = default!;

    [Column("PayloadJson")]
    public string Payload { get; set; } = default!;

    [Column("OccurredAtUtc")]
    public DateTimeOffset OccurredAtUtc { get; set; }

    [Column("SentAtUtc")]
    public DateTimeOffset? SentAtUtc { get; set; }

    [Column("AttemptCount")]
    public int AttemptCount { get; set; }

    [Column("LastError")]
    public string? LastError { get; set; }
}
