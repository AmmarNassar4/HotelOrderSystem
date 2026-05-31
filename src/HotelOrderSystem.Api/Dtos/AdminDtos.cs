using System.ComponentModel.DataAnnotations;

namespace HotelOrderSystem.Api.Dtos;

public sealed record CreateTeamRequest([property: Required, MaxLength(150)] string Name, bool IsActive = true);
public sealed record UpdateTeamRequest([property: Required, MaxLength(150)] string Name, bool IsActive = true);

public sealed record CreateRoomRequest([property: Required, MaxLength(50)] string RoomNumber, [property: MaxLength(250)] string? DirectLinkPayload, bool IsActive = true);
public sealed record UpdateRoomRequest([property: Required, MaxLength(50)] string RoomNumber, [property: MaxLength(250)] string? DirectLinkPayload, bool IsActive = true);

public sealed record CreateItemCategoryRequest([property: Required, MaxLength(150)] string Name, [property: MaxLength(500)] string? Description, bool IsActive = true);
public sealed record UpdateItemCategoryRequest([property: Required, MaxLength(150)] string Name, [property: MaxLength(500)] string? Description, bool IsActive = true);

public sealed record CreateItemRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required, MaxLength(50)] string Type,
    [property: Range(1, int.MaxValue)] int ItemCategoryId,
    int? TargetTeamId,
    string? BaseProperties,
    bool StaffOnly = false,
    bool IsActive = true);

public sealed record UpdateItemRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required, MaxLength(50)] string Type,
    [property: Range(1, int.MaxValue)] int ItemCategoryId,
    int? TargetTeamId,
    string? BaseProperties,
    bool StaffOnly = false,
    bool IsActive = true);

public sealed record CreateUserRequest(
    [property: Required, MaxLength(200)] string FullName,
    [property: Required, MaxLength(80)] string UserName,
    [property: Required, MaxLength(200)] string Password,
    int? TeamId,
    [property: Required, MaxLength(50)] string Role,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    [property: Required, MaxLength(200)] string FullName,
    int? TeamId,
    [property: Required, MaxLength(50)] string Role,
    bool IsActive = true,
    [property: MaxLength(200)] string? NewPassword = null);

public sealed record DashboardSummaryDto(
    int PendingOrders,
    int AcceptedOrders,
    int OnlineStaff,
    int ActiveRooms,
    DateTime ServerTimeUtc,
    int ReadyStaff = 0,
    int NotReadyStaff = 0);

public sealed record StatusBreakdownDto(
    string Status,
    int Count);

public sealed record TeamPerformanceDto(
    int? TeamId,
    string TeamName,
    int TotalOrders,
    int PendingOrders,
    int AcceptedOrders,
    int CompletedOrders,
    int CancelledOrders,
    int EscalatedOrders,
    double? AverageAcceptMinutes,
    double? AverageCompletionMinutes);

public sealed record StaffPerformanceDto(
    int UserId,
    string FullName,
    string? TeamName,
    int ActiveOrders,
    int CompletedOrders,
    double? AverageAcceptMinutes,
    double? AverageCompletionMinutes,
    double ReadyMinutes = 0,
    double NotReadyMinutes = 0,
    double ReadyRatePercent = 0);

public sealed record PerformanceSummaryDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalOrders,
    int PendingOrders,
    int AcceptedOrders,
    int CompletedOrders,
    int CancelledOrders,
    int EscalatedOrders,
    double CompletionRatePercent,
    double? AverageAcceptMinutes,
    double? AverageCompletionMinutes,
    IReadOnlyList<StatusBreakdownDto> ByStatus,
    IReadOnlyList<TeamPerformanceDto> ByTeam,
    IReadOnlyList<StaffPerformanceDto> ByStaff);
