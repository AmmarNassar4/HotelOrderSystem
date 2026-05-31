using System.Security.Claims;
using HotelOrderSystem.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HotelOrderSystem.Api.Hubs;

[Authorize(Roles = Roles.Admin + "," + Roles.Supervisor)]
public sealed class AdminHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

        var teamId = Context.User?.FindFirstValue("team_id");
        if (!string.IsNullOrWhiteSpace(teamId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team:{teamId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");

        var teamId = Context.User?.FindFirstValue("team_id");
        if (!string.IsNullOrWhiteSpace(teamId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team:{teamId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
