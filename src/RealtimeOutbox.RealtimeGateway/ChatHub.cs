using Microsoft.AspNetCore.SignalR;

namespace RealtimeOutbox.RealtimeGateway;

public sealed class ChatHub : Hub
{
    public async Task SendMessage(string tenantId, string channelId)
    {
        await Groups.AddToGroupAsync(
        Context.ConnectionId,
        $"{tenantId}:{channelId}");
    }

    public async Task JoinChannel(string tenantId, string channelId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"{tenantId}:{channelId}");
    }

    public async Task LeaveChannel(string tenantId, string channelId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"{tenantId}:{channelId}");
    }
}