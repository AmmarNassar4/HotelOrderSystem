using System.ComponentModel.DataAnnotations;

namespace HotelOrderSystem.Api.Dtos;

public sealed record LoginRequest(
    [property: Required, MaxLength(80)] string UserName,
    [property: Required, MaxLength(200)] string Password);

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserProfileDto User);

public sealed record UserProfileDto(
    int UserId,
    string FullName,
    string UserName,
    string Role,
    int? TeamId,
    string? TeamName);

public sealed record DeviceTokenRequest(
    [property: Required, MaxLength(200)] string DeviceId,
    [property: Required, MaxLength(50)] string Platform,
    [property: MaxLength(50)] string AppVersion,
    [property: MaxLength(1000)] string FcmToken);

public sealed record LogoutRequest(
    [property: Required, MaxLength(200)] string DeviceId);
