using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace HotelOrderSystem.Api.Dtos;

public sealed record CreateOrderRequest(
    [property: Range(1, int.MaxValue)] int RoomId,
    [property: MaxLength(50)] string Source,
    [property: Required, MinLength(1)] List<CreateOrderLineRequest> Items);

public sealed record CreateOrderLineRequest(
    [property: Range(1, int.MaxValue)] int ItemId,
    [property: Range(1, int.MaxValue)] int Quantity,
    JsonElement? DynamicAttributes);

public sealed record CreateGuestOrderRequest(
    [property: Required, MinLength(1)] List<CreateOrderLineRequest> Items);

public sealed record AcceptOrderRequest(string? RowVersion);

public sealed record CompleteOrderRequest(string? Notes);

public sealed record CancelOrderRequest(string? Reason);

public sealed record OrderDto(
    int OrderId,
    int RoomId,
    string RoomNumber,
    int? AssignedTeamId,
    string? AssignedTeamName,
    string Source,
    string Status,
    int? CreatedByUserId,
    string? CreatedByUserName,
    int? AcceptedByUserId,
    string? AcceptedByUserName,
    DateTime CreatedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? SlaDueAtUtc,
    DateTime? EscalatedAtUtc,
    string RowVersion,
    List<OrderDetailDto> Details);

public sealed record OrderDetailDto(
    int OrderDetailId,
    int ItemId,
    string ItemName,
    int Quantity,
    string DynamicAttributes);

public sealed record CreateOrderResponse(IReadOnlyList<OrderDto> Orders);
