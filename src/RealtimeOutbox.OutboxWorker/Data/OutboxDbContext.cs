using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.OutboxWorker.Entities;

namespace RealtimeOutbox.OutboxWorker.Data;

public sealed class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options){ }
    
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxEvent>(b =>
        {
           b.ToTable("outbox_events"); 
           b.HasKey(x => x.Id);
           b.Property(x => x.Type).HasMaxLength(255);
           b.Property(x => x.Payload).HasColumnType("text");
        });       
    }
}
