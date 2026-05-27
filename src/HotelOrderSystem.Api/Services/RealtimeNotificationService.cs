using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HotelOrderSystem.Api.Services;

public sealed class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<AdminHub> _adminHub;
    private readonly IHubContext<StaffHub> _staffHub;

    public RealtimeNotificationService(IHubContext<AdminHub> adminHub, IHubContext<StaffHub> staffHub)
    {
        _adminHub = adminHub;
        _staffHub = staffHub;
    }

    public async Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await _adminHub.Clients.Group("admins").SendAsync("OrderCreated", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await _staffHub.Clients.Group($"team:{order.AssignedTeamId.Value}").SendAsync("OrderCreated", order, cancellationToken);
        }
        else
        {
            await _staffHub.Clients.All.SendAsync("OrderCreated", order, cancellationToken);
        }
    }

    public async Task NotifyOrderAcceptedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await _adminHub.Clients.Group("admins").SendAsync("OrderAccepted", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await _staffHub.Clients.Group($"team:{order.AssignedTeamId.Value}").SendAsync("OrderAccepted", order, cancellationToken);
        }
        else
        {
            await _staffHub.Clients.All.SendAsync("OrderAccepted", order, cancellationToken);
        }
    }

    public async Task NotifyOrderCompletedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await _adminHub.Clients.Group("admins").SendAsync("OrderCompleted", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await _staffHub.Clients.Group($"team:{order.AssignedTeamId.Value}").SendAsync("OrderCompleted", order, cancellationToken);
        }
    }

    public async Task NotifyStaffPresenceChangedAsync(int userId, bool isOnline, CancellationToken cancellationToken = default)
    {
        await _adminHub.Clients.Group("admins").SendAsync("StaffPresenceChanged", new { userId, isOnline }, cancellationToken);
    }

    public async Task NotifyDashboardChangedAsync(CancellationToken cancellationToken = default)
    {
        await _adminHub.Clients.Group("admins").SendAsync("DashboardChanged", new { serverTimeUtc = DateTime.UtcNow }, cancellationToken);
    }
}
