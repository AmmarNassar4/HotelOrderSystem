using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Entities;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserEntity = HotelOrderSystem.Api.Entities.User;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly ICatalogService _catalog;

    public AdminController(AppDbContext db, IPasswordService passwordService, ICatalogService catalog)
    {
        _db = db;
        _passwordService = passwordService;
        _catalog = catalog;
    }

    [HttpGet("dashboard/live-summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<DashboardSummaryDto>.Success(new DashboardSummaryDto(
            await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Pending, cancellationToken),
            await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress, cancellationToken),
            await _db.UserPresences.CountAsync(x => x.IsOnline, cancellationToken),
            await _db.Rooms.CountAsync(x => x.IsActive, cancellationToken),
            DateTime.UtcNow)));
    }

    [HttpGet("teams")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TeamDto>>>> GetTeams(CancellationToken cancellationToken)
    {
        var rows = await _db.Teams.AsNoTracking().OrderBy(x => x.Name).Select(x => new TeamDto(x.TeamId, x.Name, x.IsActive)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TeamDto>>.Success(rows));
    }

    [HttpPost("teams")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> CreateTeam(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = new Team { Name = request.Name.Trim(), IsActive = request.IsActive };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<TeamDto>.Success(new TeamDto(team.TeamId, team.Name, team.IsActive)));
    }

    [HttpPut("teams/{id:int}")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> UpdateTeam(int id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(x => x.TeamId == id, cancellationToken);
        if (team is null) return NotFound(ApiResponse<TeamDto>.Fail("Team not found."));
        team.Name = request.Name.Trim();
        team.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<TeamDto>.Success(new TeamDto(team.TeamId, team.Name, team.IsActive)));
    }

    [HttpDelete("teams/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteTeam(int id, CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(x => x.TeamId == id, cancellationToken);
        if (team is null) return NotFound(ApiResponse.Fail("Team not found."));
        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetRooms(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<IReadOnlyList<RoomDto>>.Success(await _catalog.GetRoomsAsync(cancellationToken)));
    }

    [HttpPost("rooms")]
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

    [HttpPut("rooms/{id:int}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> UpdateRoom(int id, UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(x => x.RoomId == id, cancellationToken);
        if (room is null) return NotFound(ApiResponse<RoomDto>.Fail("Room not found."));
        room.RoomNumber = request.RoomNumber.Trim();
        room.DirectLinkPayload = string.IsNullOrWhiteSpace(request.DirectLinkPayload) ? room.DirectLinkPayload : request.DirectLinkPayload.Trim();
        room.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<RoomDto>.Success(new RoomDto(room.RoomId, room.RoomNumber, room.DirectLinkPayload, room.IsActive)));
    }

    [HttpDelete("rooms/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteRoom(int id, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(x => x.RoomId == id, cancellationToken);
        if (room is null) return NotFound(ApiResponse.Fail("Room not found."));
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("item-categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemCategoryDto>>>> GetItemCategories(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<IReadOnlyList<ItemCategoryDto>>.Success(await _catalog.GetItemCategoriesAsync(cancellationToken)));
    }

    [HttpPost("item-categories")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> CreateItemCategory(CreateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = new ItemCategory { Name = request.Name.Trim(), Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(), IsActive = request.IsActive };
        _db.ItemCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<ItemCategoryDto>.Success(new ItemCategoryDto(category.ItemCategoryId, category.Name, category.Description, category.IsActive)));
    }

    [HttpPut("item-categories/{id:int}")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> UpdateItemCategory(int id, UpdateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _db.ItemCategories.FirstOrDefaultAsync(x => x.ItemCategoryId == id, cancellationToken);
        if (category is null) return NotFound(ApiResponse<ItemCategoryDto>.Fail("Category not found."));
        category.Name = request.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<ItemCategoryDto>.Success(new ItemCategoryDto(category.ItemCategoryId, category.Name, category.Description, category.IsActive)));
    }

    [HttpDelete("item-categories/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteItemCategory(int id, CancellationToken cancellationToken)
    {
        var category = await _db.ItemCategories.FirstOrDefaultAsync(x => x.ItemCategoryId == id, cancellationToken);
        if (category is null) return NotFound(ApiResponse.Fail("Category not found."));
        if (await _db.Items.AnyAsync(x => x.ItemCategoryId == id, cancellationToken)) return BadRequest(ApiResponse.Fail("Category is linked to one or more items. Move or delete those items first."));
        _db.ItemCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("items")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemDto>>>> GetItems(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<IReadOnlyList<ItemDto>>.Success(await _catalog.GetItemsAsync(cancellationToken)));
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> CreateItem(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateCategoryAndJson(request.ItemCategoryId, request.BaseProperties, cancellationToken);
        if (validation is not null) return BadRequest(ApiResponse<ItemDto>.Fail(validation.Value.Error));
        var item = new Item { Name = request.Name.Trim(), Type = request.Type.Trim(), ItemCategoryId = request.ItemCategoryId, TargetTeamId = request.TargetTeamId, BaseProperties = validation!.Value.Json, IsActive = request.IsActive };
        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<ItemDto>.Success(await LoadItemDto(item.ItemId, cancellationToken)));
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> UpdateItem(int id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);
        if (item is null) return NotFound(ApiResponse<ItemDto>.Fail("Item not found."));
        var validation = await ValidateCategoryAndJson(request.ItemCategoryId, request.BaseProperties, cancellationToken);
        if (validation is not null) return BadRequest(ApiResponse<ItemDto>.Fail(validation.Value.Error));
        item.Name = request.Name.Trim();
        item.Type = request.Type.Trim();
        item.ItemCategoryId = request.ItemCategoryId;
        item.TargetTeamId = request.TargetTeamId;
        item.BaseProperties = validation!.Value.Json;
        item.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse<ItemDto>.Success(await LoadItemDto(item.ItemId, cancellationToken)));
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteItem(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);
        if (item is null) return NotFound(ApiResponse.Fail("Item not found."));
        _db.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserProfileDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var rows = await _db.Users.AsNoTracking().Include(x => x.Team).OrderBy(x => x.FullName).Select(x => new UserProfileDto(x.UserId, x.FullName, x.UserName, x.Role, x.TeamId, x.Team != null ? x.Team.Name : null)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserProfileDto>>.Success(rows));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new UserEntity { FullName = request.FullName.Trim(), UserName = request.UserName.Trim(), TeamId = request.TeamId, Role = request.Role.Trim(), IsActive = request.IsActive };
        user.PasswordHash = _passwordService.HashPassword(user, request.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        var created = await _db.Users.AsNoTracking().Include(x => x.Team).FirstAsync(x => x.UserId == user.UserId, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Success(new UserProfileDto(created.UserId, created.FullName, created.UserName, created.Role, created.TeamId, created.Team?.Name)));
    }

    [HttpPut("users/{id:int}")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateUser(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
        if (user is null) return NotFound(ApiResponse<UserProfileDto>.Fail("User not found."));
        user.FullName = request.FullName.Trim();
        user.TeamId = request.TeamId;
        user.Role = request.Role.Trim();
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.NewPassword)) user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
        var updated = await _db.Users.AsNoTracking().Include(x => x.Team).FirstAsync(x => x.UserId == id, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Success(new UserProfileDto(updated.UserId, updated.FullName, updated.UserName, updated.Role, updated.TeamId, updated.Team?.Name)));
    }

    [HttpDelete("users/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
        if (user is null) return NotFound(ApiResponse.Fail("User not found."));
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> GetOrders([FromQuery] string? status, [FromQuery] int? roomId, [FromQuery] int? teamId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] int take = 300, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 1000);
        var query = _db.Orders.AsNoTracking().Include(x => x.Room).Include(x => x.AssignedTeam).Include(x => x.CreatedByUser).Include(x => x.AcceptedByUser).Include(x => x.Details).ThenInclude(x => x.Item).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (roomId.HasValue) query = query.Where(x => x.RoomId == roomId.Value);
        if (teamId.HasValue) query = query.Where(x => x.AssignedTeamId == teamId.Value);
        if (fromUtc.HasValue) query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.CreatedAt <= toUtc.Value);
        var orders = await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderDto>>.Success(orders.Select(MapOrder).ToList()));
    }

    [HttpGet("performance")]
    public async Task<ActionResult<ApiResponse<PerformanceSummaryDto>>> GetPerformance([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
    {
        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-7);
        var orders = await _db.Orders.AsNoTracking().Include(x => x.AssignedTeam).Include(x => x.AcceptedByUser).ThenInclude(x => x!.Team).Where(x => x.CreatedAt >= from && x.CreatedAt <= to).ToListAsync(cancellationToken);
        var total = orders.Count;
        var completed = orders.Count(x => x.Status == OrderStatuses.Completed);
        var byStatus = orders.GroupBy(x => x.Status).Select(x => new StatusBreakdownDto(x.Key, x.Count())).OrderByDescending(x => x.Count).ToList();
        var byTeam = orders.GroupBy(x => new { x.AssignedTeamId, TeamName = x.AssignedTeam != null ? x.AssignedTeam.Name : "All Teams" }).Select(g => new TeamPerformanceDto(g.Key.AssignedTeamId, g.Key.TeamName, g.Count(), g.Count(x => x.Status == OrderStatuses.Pending), g.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress), g.Count(x => x.Status == OrderStatuses.Completed), g.Count(x => x.Status == OrderStatuses.Cancelled), g.Count(x => x.EscalatedAt.HasValue), AverageMinutes(g.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)), AverageMinutes(g.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt)))).OrderByDescending(x => x.TotalOrders).ToList();
        var byStaff = orders.Where(x => x.AcceptedByUserId.HasValue && x.AcceptedByUser != null).GroupBy(x => new { UserId = x.AcceptedByUserId!.Value, x.AcceptedByUser!.FullName, TeamName = x.AcceptedByUser.Team != null ? x.AcceptedByUser.Team.Name : null }).Select(g => new StaffPerformanceDto(g.Key.UserId, g.Key.FullName, g.Key.TeamName, g.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress), g.Count(x => x.Status == OrderStatuses.Completed), AverageMinutes(g.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)), AverageMinutes(g.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt)))).OrderByDescending(x => x.CompletedOrders).ToList();
        var response = new PerformanceSummaryDto(from, to, total, orders.Count(x => x.Status == OrderStatuses.Pending), orders.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress), completed, orders.Count(x => x.Status == OrderStatuses.Cancelled), orders.Count(x => x.EscalatedAt.HasValue), total == 0 ? 0 : Math.Round(completed * 100.0 / total, 2), AverageMinutes(orders.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)), AverageMinutes(orders.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt)), byStatus, byTeam, byStaff);
        return Ok(ApiResponse<PerformanceSummaryDto>.Success(response));
    }

    private async Task<ItemDto> LoadItemDto(int itemId, CancellationToken cancellationToken)
    {
        var item = await _db.Items.AsNoTracking().Include(x => x.Category).Include(x => x.TargetTeam).FirstAsync(x => x.ItemId == itemId, cancellationToken);
        return new ItemDto(item.ItemId, item.Name, item.Type, item.TargetTeamId, item.TargetTeam?.Name, item.BaseProperties, item.IsActive, item.ItemCategoryId, item.Category?.Name);
    }

    private async Task<(string Json, string Error)?> ValidateCategoryAndJson(int categoryId, string? baseProperties, CancellationToken cancellationToken)
    {
        var json = JsonValidator.NormalizeObjectJson(baseProperties);
        if (!JsonValidator.IsValidObjectJson(json)) return (json, "Item property schema must be a valid JSON object.");
        if (!await _db.ItemCategories.AnyAsync(x => x.ItemCategoryId == categoryId && x.IsActive, cancellationToken)) return (json, "A valid active category is required before saving an item.");
        return (json, string.Empty);
    }

    private static double? AverageMinutes(IEnumerable<TimeSpan> values)
    {
        var list = values.Select(x => x.TotalMinutes).Where(x => x >= 0).ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 2);
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto(order.OrderId, order.RoomId, order.Room.RoomNumber, order.AssignedTeamId, order.AssignedTeam?.Name, order.Source, order.Status, order.CreatedByUserId, order.CreatedByUser?.FullName, order.AcceptedByUserId, order.AcceptedByUser?.FullName, order.CreatedAt, order.AcceptedAt, order.CompletedAt, order.SlaDueAt, order.EscalatedAt, Convert.ToBase64String(order.RowVersion), order.Details.Select(detail => new OrderDetailDto(detail.OrderDetailId, detail.ItemId, detail.Item.Name, detail.Quantity, detail.DynamicAttributes)).ToList());
    }
}
