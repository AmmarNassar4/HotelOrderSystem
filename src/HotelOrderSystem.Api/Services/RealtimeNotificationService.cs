using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HotelOrderSystem.Api.Services;

public sealed class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<AdminHub> _adminHub;
    private readonly IHubContext<StaffHub> _staffHub;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(IHubContext<AdminHub> adminHub, IHubContext<StaffHub> staffHub, ILogger<RealtimeNotificationService> logger)
    {
        _adminHub = adminHub;
        _staffHub = staffHub;
        _logger = logger;
    }

    public async Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await SendToAdminsAsync("OrderCreated", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await SendToTeamAsync(order.AssignedTeamId.Value, "OrderCreated", order, cancellationToken);
        }
        else
        {
            await SendToAllStaffAsync("OrderCreated", order, cancellationToken);
        }
    }

    public async Task NotifyOrderAcceptedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await SendToAdminsAsync("OrderAccepted", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await SendToTeamAsync(order.AssignedTeamId.Value, "OrderAccepted", order, cancellationToken);
        }
        else
        {
            await SendToAllStaffAsync("OrderAccepted", order, cancellationToken);
        }
    }

    public async Task NotifyOrderCompletedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await SendToAdminsAsync("OrderCompleted", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await SendToTeamAsync(order.AssignedTeamId.Value, "OrderCompleted", order, cancellationToken);
        }
    }

    public async Task NotifyOrderCancelledAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        await SendToAdminsAsync("OrderCancelled", order, cancellationToken);

        if (order.AssignedTeamId.HasValue)
        {
            await SendToTeamAsync(order.AssignedTeamId.Value, "OrderCancelled", order, cancellationToken);
        }
        else
        {
            await SendToAllStaffAsync("OrderCancelled", order, cancellationToken);
        }
    }

    public async Task NotifyStaffPresenceChangedAsync(int userId, bool isOnline, CancellationToken cancellationToken = default)
    {
        await SendToAdminsAsync("StaffPresenceChanged", new { userId, isOnline }, cancellationToken);
    }

    public async Task NotifyDashboardChangedAsync(CancellationToken cancellationToken = default)
    {
        await SendToAdminsAsync("DashboardChanged", new { serverTimeUtc = DateTime.UtcNow }, cancellationToken);
    }

    private async Task SendToAdminsAsync(string method, object arg, CancellationToken cancellationToken)
    {
        try
        {
            await _adminHub.Clients.Group("admins").SendAsync(method, arg, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR send to admins failed for {Method}", method);
        }
    }

    private async Task SendToTeamAsync(int teamId, string method, object arg, CancellationToken cancellationToken)
    {
        try
        {
            await _staffHub.Clients.Group($"team:{teamId}").SendAsync(method, arg, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR send to team {TeamId} failed for {Method}", teamId, method);
        }
    }

    private async Task SendToAllStaffAsync(string method, object arg, CancellationToken cancellationToken)
    {
        try
        {
            await _staffHub.Clients.All.SendAsync(method, arg, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast to all staff failed for {Method}", method);
        }
    }
}
