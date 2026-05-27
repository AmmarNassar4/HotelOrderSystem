namespace HotelOrderSystem.Api.Services;

public interface IPushNotificationService
{
    Task SendToUserAsync(int userId, string type, string payloadJson, CancellationToken cancellationToken = default);
    Task SendToTeamAsync(int teamId, string type, string payloadJson, CancellationToken cancellationToken = default);
    Task SendToAdminsAsync(string type, string payloadJson, CancellationToken cancellationToken = default);
    Task SendBroadcastAsync(string type, string payloadJson, CancellationToken cancellationToken = default);
}
