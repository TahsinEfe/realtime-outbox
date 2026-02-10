using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.ChatService.Contracts;
using RealtimeOutbox.ChatService.Entities;


namespace RealtimeOutbox.ChatService.Data;

public sealed class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options): base(options) {  }

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(b =>
        {
            b.ToTable("messages");
            b.HasKey(x => x.MessageId);
            b.Property(x => x.Content).HasMaxLength(4000);
            b.HasIndex(x => new { x.TenantId, x.ChannelId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<OutboxEvent>(b =>
        {
            b.ToTable("outbox_events");
            b.HasKey(x => x.OutboxEventId);

            b.Property(x => x.Type).HasMaxLength(200);
            b.Property(x => x.PayloadJson).HasColumnType("text");

            b.HasIndex(x => new { x.SentAtUtc, x.OccurredAtUtc });
            b.HasIndex(x => new { x.TenantId, x.SentAtUtc });
        });
    }
}
