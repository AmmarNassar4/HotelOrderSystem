namespace HotelOrderSystem.Api.Dtos;

public sealed record CreateTeamRequest(string Name, bool IsActive = true);
public sealed record UpdateTeamRequest(string Name, bool IsActive = true);

public sealed record CreateRoomRequest(string RoomNumber, string? DirectLinkPayload, bool IsActive = true);
public sealed record UpdateRoomRequest(string RoomNumber, string? DirectLinkPayload, bool IsActive = true);

public sealed record CreateItemCategoryRequest(string Name, string? Description, bool IsActive = true);
public sealed record UpdateItemCategoryRequest(string Name, string? Description, bool IsActive = true);

public sealed record CreateItemRequest(
    string Name,
    string Type,
    int ItemCategoryId,
    int? TargetTeamId,
    string? BaseProperties,
    bool IsActive = true);

public sealed record UpdateItemRequest(
    string Name,
    string Type,
    int ItemCategoryId,
    int? TargetTeamId,
    string? BaseProperties,
    bool IsActive = true);

public sealed record CreateUserRequest(
    string FullName,
    string UserName,
    string Password,
    int? TeamId,
    string Role,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    string FullName,
    int? TeamId,
    string Role,
    bool IsActive = true,
    string? NewPassword = null);

public sealed record DashboardSummaryDto(
    int PendingOrders,
    int AcceptedOrders,
    int OnlineStaff,
    int ReadyStaff,
    int NotReadyStaff,
    int ActiveRooms,
    DateTime ServerTimeUtc);

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
    double ReadyMinutes,
    double NotReadyMinutes,
    double ReadyRatePercent);

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
