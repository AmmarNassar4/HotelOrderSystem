using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/items")]
public sealed class ItemsController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public ItemsController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemDto>>>> GetItems(CancellationToken cancellationToken)
    {
        var items = await _catalog.GetItemsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ItemDto>>.Success(items));
    }
}
