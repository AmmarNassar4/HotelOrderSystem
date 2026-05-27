namespace HotelOrderSystem.Api.Dtos;

public sealed record LoginRequest(string UserName, string Password);

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserProfileDto User);

public sealed record UserProfileDto(
    int UserId,
    string FullName,
    string UserName,
    string Role,
    int? TeamId,
    string? TeamName);

public sealed record DeviceTokenRequest(
    string DeviceId,
    string Platform,
    string AppVersion,
    string FcmToken);

public sealed record LogoutRequest(string DeviceId);
