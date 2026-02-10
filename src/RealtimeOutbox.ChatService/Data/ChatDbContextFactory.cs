using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RealtimeOutbox.ChatService.Data;

public sealed class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var cs = "Host=localhost;Port=5433;Database=realtime-outbox_chat;Username=realtime-outbox;Password=realtime-outbox_pass";
    
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new ChatDbContext(options);
    }
}