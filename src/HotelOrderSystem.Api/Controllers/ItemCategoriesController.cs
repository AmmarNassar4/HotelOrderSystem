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
[Route("api/v1/admin/item-categories")]
public sealed class ItemCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICatalogService _catalog;

    public ItemCategoriesController(AppDbContext db, ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemCategoryDto>>>> GetItemCategories(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<IReadOnlyList<ItemCategoryDto>>.Success(await _catalog.GetItemCategoriesAsync(cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> CreateItemCategory(CreateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = new ItemCategory
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive
        };

        _db.ItemCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<ItemCategoryDto>.Success(new ItemCategoryDto(category.ItemCategoryId, category.Name, category.Description, category.IsActive)));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> UpdateItemCategory(int id, UpdateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _db.ItemCategories.FirstOrDefaultAsync(x => x.ItemCategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound(ApiResponse<ItemCategoryDto>.Fail("Category not found."));
        }

        category.Name = request.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<ItemCategoryDto>.Success(new ItemCategoryDto(category.ItemCategoryId, category.Name, category.Description, category.IsActive)));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteItemCategory(int id, CancellationToken cancellationToken)
    {
        var category = await _db.ItemCategories.FirstOrDefaultAsync(x => x.ItemCategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound(ApiResponse.Fail("Category not found."));
        }

        var hasItems = await _db.Items.AnyAsync(x => x.ItemCategoryId == id, cancellationToken);
        if (hasItems)
        {
            return BadRequest(ApiResponse.Fail("Category is linked to one or more items. Move or delete those items first."));
        }

        _db.ItemCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse.Success(new { deleted = true }));
    }
}
