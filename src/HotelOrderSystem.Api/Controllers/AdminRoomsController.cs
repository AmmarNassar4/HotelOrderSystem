using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Entities;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/rooms")]
public sealed class AdminRoomsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICatalogService _catalog;

    public AdminRoomsController(AppDbContext db, ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetRooms(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<IReadOnlyList<RoomDto>>.Success(await _catalog.GetRoomsAsync(cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoomDto>>> CreateRoom(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = new Room
        {
            RoomNumber = request.RoomNumber.Trim(),
            DirectLinkPayload = string.IsNullOrWhiteSpace(request.DirectLinkPayload) ? Guid.NewGuid().ToString("N") : request.DirectLinkPayload.Trim(),
            IsActive = request.IsActive
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<RoomDto>.Success(new RoomDto(room.RoomId, room.RoomNumber, room.DirectLinkPayload, room.IsActive)));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> UpdateRoom(int id, UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(x => x.RoomId == id, cancellationToken);
        if (room is null)
        {
            return NotFound(ApiResponse<RoomDto>.Fail("Room not found."));
        }

        room.RoomNumber = request.RoomNumber.Trim();
        room.DirectLinkPayload = string.IsNullOrWhiteSpace(request.DirectLinkPayload) ? room.DirectLinkPayload : request.DirectLinkPayload.Trim();
        room.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<RoomDto>.Success(new RoomDto(room.RoomId, room.RoomNumber, room.DirectLinkPayload, room.IsActive)));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteRoom(int id, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(x => x.RoomId == id, cancellationToken);
        if (room is null)
        {
            return NotFound(ApiResponse.Fail("Room not found."));
        }

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse.Success(new { deleted = true }));
    }
}
