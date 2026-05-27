using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HotelOrderSystem.Api.Hubs;

[Authorize]
public sealed class StaffHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var teamId = Context.User?.FindFirstValue("team_id");
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(teamId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team:{teamId}");
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var teamId = Context.User?.FindFirstValue("team_id");
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(teamId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team:{teamId}");
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
