using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HotelOrderSystem.Api.Services;

public sealed class CatalogService : ICatalogService
{
    private const string RoomsCacheKey = "catalog:rooms";
    private const string ItemsCacheKey = "catalog:items";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public CatalogService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(RoomsCacheKey, out IReadOnlyList<RoomDto>? cached) && cached is not null)
        {
            return cached;
        }

        var rooms = await _db.Rooms
            .AsNoTracking()
            .OrderBy(x => x.RoomNumber)
            .Select(x => new RoomDto(x.RoomId, x.RoomNumber, x.DirectLinkPayload, x.IsActive))
            .ToListAsync(cancellationToken);

        _cache.Set(RoomsCacheKey, rooms, TimeSpan.FromMinutes(5));
        return rooms;
    }

    public async Task<IReadOnlyList<ItemDto>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(ItemsCacheKey, out IReadOnlyList<ItemDto>? cached) && cached is not null)
        {
            return cached;
        }

        var items = await _db.Items
            .AsNoTracking()
            .Include(x => x.TargetTeam)
            .OrderBy(x => x.Name)
            .Select(x => new ItemDto(x.ItemId, x.Name, x.Type, x.TargetTeamId, x.TargetTeam != null ? x.TargetTeam.Name : null, x.BaseProperties, x.IsActive))
            .ToListAsync(cancellationToken);

        _cache.Set(ItemsCacheKey, items, TimeSpan.FromMinutes(5));
        return items;
    }

    public void ClearCache()
    {
        _cache.Remove(RoomsCacheKey);
        _cache.Remove(ItemsCacheKey);
    }
}
