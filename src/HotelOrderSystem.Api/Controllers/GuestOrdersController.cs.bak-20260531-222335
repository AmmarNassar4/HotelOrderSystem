using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/guest")]
public sealed class GuestOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orders;
    private readonly ICatalogService _catalog;

    public GuestOrdersController(AppDbContext db, IOrderService orders, ICatalogService catalog)
    {
        _db = db;
        _orders = orders;
        _catalog = catalog;
    }

    [HttpGet("rooms/{directLinkPayload}/catalog")]
    public async Task<ActionResult<ApiResponse<GuestCatalogDto>>> GetGuestCatalog(string directLinkPayload, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DirectLinkPayload == directLinkPayload && x.IsActive, cancellationToken);

        if (room is null)
        {
            return NotFound(ApiResponse<GuestCatalogDto>.Fail("Room link is invalid or inactive."));
        }

        var items = await _catalog.GetItemsAsync(cancellationToken);
        var response = new GuestCatalogDto(
            new RoomDto(room.RoomId, room.RoomNumber, room.DirectLinkPayload, room.IsActive),
            items.Where(x => x.IsActive).ToList());

        return Ok(ApiResponse<GuestCatalogDto>.Success(response));
    }

    [HttpPost("rooms/{directLinkPayload}/orders")]
    public async Task<ActionResult<ApiResponse<CreateOrderResponse>>> CreateGuestOrder(string directLinkPayload, CreateGuestOrderRequest request, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DirectLinkPayload == directLinkPayload && x.IsActive, cancellationToken);

        if (room is null)
        {
            return NotFound(ApiResponse<CreateOrderResponse>.Fail("Room link is invalid or inactive."));
        }

        var createRequest = new CreateOrderRequest(room.RoomId, OrderSources.GuestQr, request.Items);
        var result = await _orders.CreateAsync(createRequest, null, string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
