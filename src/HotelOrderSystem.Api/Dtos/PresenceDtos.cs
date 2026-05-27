namespace HotelOrderSystem.Api.Dtos;

public sealed record HeartbeatRequest(
    string DeviceId,
    string AppState,
    string? CurrentScreen);

public sealed record HeartbeatResponse(
    DateTime ServerTimeUtc,
    bool Online,
    int PendingOrdersCount);

public sealed record StaffPresenceDto(
    int UserId,
    string FullName,
    int? TeamId,
    string? TeamName,
    bool IsOnline,
    DateTime? LastHeartbeatAtUtc,
    string? LastKnownAppState);
