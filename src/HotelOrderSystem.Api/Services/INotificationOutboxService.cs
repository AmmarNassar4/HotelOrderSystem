namespace HotelOrderSystem.Api.Services;

public interface INotificationOutboxService
{
    Task EnqueueAsync(string type, int? targetUserId, int? targetTeamId, string payloadJson, CancellationToken cancellationToken = default);
}
