using System.ComponentModel.DataAnnotations;

namespace HotelOrderSystem.Api.Dtos;

public sealed record HeartbeatRequest(
    [property: Required, MaxLength(200)] string DeviceId,
    [property: MaxLength(50)] string AppState,
    [property: MaxLength(100)] string? CurrentScreen);

public sealed record HeartbeatResponse(
    DateTime ServerTimeUtc,
    bool Online,
    bool IsReady,
    int PendingOrdersCount);

public sealed record AvailabilityRequest(
    bool IsReady,
    string? DeviceId,
    string? Source);

public sealed record AvailabilityResponse(
    bool IsReady,
    DateTime? ReadySinceAtUtc,
    DateTime ChangedAtUtc);

public sealed record StaffPresenceDto(
    int UserId,
    string FullName,
    int? TeamId,
    string? TeamName,
    bool IsOnline,
    bool IsReady,
    DateTime? ReadySinceAtUtc,
    DateTime? LastHeartbeatAtUtc,
    string? LastKnownAppState);
