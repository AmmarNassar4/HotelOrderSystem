using HotelOrderSystem.Api.Dtos;

namespace HotelOrderSystem.Api.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<RoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemDto>> GetItemsAsync(CancellationToken cancellationToken = default);
    void ClearCache();
}
