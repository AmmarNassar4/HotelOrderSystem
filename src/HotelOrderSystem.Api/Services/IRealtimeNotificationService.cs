using HotelOrderSystem.Api.Dtos;

namespace HotelOrderSystem.Api.Services;

public interface IRealtimeNotificationService
{
    Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyOrderAcceptedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyOrderCompletedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyOrderCancelledAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyStaffPresenceChangedAsync(int userId, bool isOnline, CancellationToken cancellationToken = default);
    Task NotifyDashboardChangedAsync(CancellationToken cancellationToken = default);
}
