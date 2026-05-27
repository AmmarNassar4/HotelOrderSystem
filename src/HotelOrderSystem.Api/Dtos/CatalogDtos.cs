namespace HotelOrderSystem.Api.Dtos;

public sealed record RoomDto(
    int RoomId,
    string RoomNumber,
    string DirectLinkPayload,
    bool IsActive);

public sealed record TeamDto(
    int TeamId,
    string Name,
    bool IsActive);

public sealed record ItemCategoryDto(
    int ItemCategoryId,
    string Name,
    string? Description,
    bool IsActive);

public sealed record ItemDto(
    int ItemId,
    string Name,
    string Type,
    int? TargetTeamId,
    string? TargetTeamName,
    string BaseProperties,
    bool IsActive,
    int? ItemCategoryId = null,
    string? ItemCategoryName = null);

public sealed record GuestCatalogDto(
    RoomDto Room,
    IReadOnlyList<ItemDto> Items,
    IReadOnlyList<ItemCategoryDto>? Categories = null);
