using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public RoomsController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetRooms(CancellationToken cancellationToken)
    {
        var rooms = await _catalog.GetRoomsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoomDto>>.Success(rooms));
    }
}
