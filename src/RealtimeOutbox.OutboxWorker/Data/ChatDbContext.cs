using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.OutboxWorker.Entities;

namespace RealtimeOutbox.OutboxWorker.Data;

public sealed class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<OutboxEvent>();

        e.ToTable("outbox_events");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("OutboxEventId");
        e.Property(x => x.TenantId).HasColumnName("TenantId");
        e.Property(x => x.Type).HasColumnName("Type").HasMaxLength(200);
        e.Property(x => x.Payload).HasColumnName("PayloadJson");
        e.Property(x => x.OccurredAtUtc).HasColumnName("OccurredAtUtc");
        e.Property(x => x.SentAtUtc).HasColumnName("SentAtUtc");
        e.Property(x => x.AttemptCount).HasColumnName("AttemptCount");
        e.Property(x => x.LastError).HasColumnName("LastError");
    }
}
