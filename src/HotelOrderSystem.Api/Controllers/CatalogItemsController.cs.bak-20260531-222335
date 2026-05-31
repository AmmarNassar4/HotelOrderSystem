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
[Route("api/v1/admin/catalog-items")]
[Route("api/v1/admin/items")]
public sealed class CatalogItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICatalogService _catalog;

    public CatalogItemsController(AppDbContext db, ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemDto>>>> GetItems(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<IReadOnlyList<ItemDto>>.Success(await _catalog.GetItemsAsync(cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemDto>>> CreateItem(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateItemRequest(request.ItemCategoryId, request.BaseProperties, cancellationToken);
        if (validation.Error is not null)
        {
            return BadRequest(ApiResponse<ItemDto>.Fail(validation.Error));
        }

        var item = new Item
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            ItemCategoryId = request.ItemCategoryId,
            TargetTeamId = request.TargetTeamId,
            BaseProperties = validation.Json,
            IsActive = request.IsActive
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<ItemDto>.Success(await LoadItemDto(item.ItemId, cancellationToken)));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> UpdateItem(int id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<ItemDto>.Fail("Item not found."));
        }

        var validation = await ValidateItemRequest(request.ItemCategoryId, request.BaseProperties, cancellationToken);
        if (validation.Error is not null)
        {
            return BadRequest(ApiResponse<ItemDto>.Fail(validation.Error));
        }

        item.Name = request.Name.Trim();
        item.Type = request.Type.Trim();
        item.ItemCategoryId = request.ItemCategoryId;
        item.TargetTeamId = request.TargetTeamId;
        item.BaseProperties = validation.Json;
        item.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<ItemDto>.Success(await LoadItemDto(item.ItemId, cancellationToken)));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteItem(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse.Fail("Item not found."));
        }

        _db.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    private async Task<(string Json, string? Error)> ValidateItemRequest(int itemCategoryId, string? baseProperties, CancellationToken cancellationToken)
    {
        var json = JsonValidator.NormalizeObjectJson(baseProperties);
        if (!JsonValidator.IsValidObjectJson(json))
        {
            return (json, "Item property schema must be a valid JSON object.");
        }

        var categoryExists = await _db.ItemCategories.AnyAsync(x => x.ItemCategoryId == itemCategoryId && x.IsActive, cancellationToken);
        if (!categoryExists)
        {
            return (json, "A valid active category is required before saving an item.");
        }

        return (json, null);
    }

    private async Task<ItemDto> LoadItemDto(int itemId, CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.TargetTeam)
            .FirstAsync(x => x.ItemId == itemId, cancellationToken);

        return new ItemDto(
            item.ItemId,
            item.Name,
            item.Type,
            item.TargetTeamId,
            item.TargetTeam?.Name,
            item.BaseProperties,
            item.IsActive,
            item.ItemCategoryId,
            item.Category?.Name);
    }
}
