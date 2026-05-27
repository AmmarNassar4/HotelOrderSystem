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
        var pending = await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Pending, cancellationToken);
        var accepted = await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress, cancellationToken);
        var online = await _db.UserPresences.CountAsync(x => x.IsOnline, cancellationToken);
        var activeRooms = await _db.Rooms.CountAsync(x => x.IsActive, cancellationToken);

        return Ok(ApiResponse<DashboardSummaryDto>.Success(new DashboardSummaryDto(
            pending,
            accepted,
            online,
            activeRooms,
            DateTime.UtcNow)));
    }

    [HttpGet("teams")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TeamDto>>>> GetTeams(CancellationToken cancellationToken)
    {
        var teams = await _db.Teams
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TeamDto(x.TeamId, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<TeamDto>>.Success(teams));
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
        if (team is null)
        {
            return NotFound(ApiResponse<TeamDto>.Fail("Team not found."));
        }

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
        if (team is null)
        {
            return NotFound(ApiResponse.Fail("Team not found."));
        }

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetRooms(CancellationToken cancellationToken)
    {
        var rooms = await _catalog.GetRoomsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoomDto>>.Success(rooms));
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

    [HttpDelete("rooms/{id:int}")]
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

    [HttpGet("items")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemDto>>>> GetItems(CancellationToken cancellationToken)
    {
        var items = await _catalog.GetItemsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ItemDto>>.Success(items));
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> CreateItem(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var baseProperties = JsonValidator.NormalizeObjectJson(request.BaseProperties);
        if (!JsonValidator.IsValidObjectJson(baseProperties))
        {
            return BadRequest(ApiResponse<ItemDto>.Fail("Item property schema must be a valid JSON object."));
        }

        var item = new Item
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            TargetTeamId = request.TargetTeamId,
            BaseProperties = baseProperties,
            IsActive = request.IsActive
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        var created = await _db.Items.AsNoTracking().Include(x => x.TargetTeam).FirstAsync(x => x.ItemId == item.ItemId, cancellationToken);
        return Ok(ApiResponse<ItemDto>.Success(new ItemDto(created.ItemId, created.Name, created.Type, created.TargetTeamId, created.TargetTeam?.Name, created.BaseProperties, created.IsActive)));
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> UpdateItem(int id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var baseProperties = JsonValidator.NormalizeObjectJson(request.BaseProperties);
        if (!JsonValidator.IsValidObjectJson(baseProperties))
        {
            return BadRequest(ApiResponse<ItemDto>.Fail("Item property schema must be a valid JSON object."));
        }

        var item = await _db.Items.FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<ItemDto>.Fail("Item not found."));
        }

        item.Name = request.Name.Trim();
        item.Type = request.Type.Trim();
        item.TargetTeamId = request.TargetTeamId;
        item.BaseProperties = baseProperties;
        item.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        var updated = await _db.Items.AsNoTracking().Include(x => x.TargetTeam).FirstAsync(x => x.ItemId == item.ItemId, cancellationToken);
        return Ok(ApiResponse<ItemDto>.Success(new ItemDto(updated.ItemId, updated.Name, updated.Type, updated.TargetTeamId, updated.TargetTeam?.Name, updated.BaseProperties, updated.IsActive)));
    }

    [HttpDelete("items/{id:int}")]
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

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserProfileDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Include(x => x.Team)
            .OrderBy(x => x.FullName)
            .Select(x => new UserProfileDto(x.UserId, x.FullName, x.UserName, x.Role, x.TeamId, x.Team != null ? x.Team.Name : null))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<UserProfileDto>>.Success(users));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new UserEntity
        {
            FullName = request.FullName.Trim(),
            UserName = request.UserName.Trim(),
            TeamId = request.TeamId,
            Role = request.Role.Trim(),
            IsActive = request.IsActive
        };
        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.Users.AsNoTracking().Include(x => x.Team).FirstAsync(x => x.UserId == user.UserId, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Success(new UserProfileDto(created.UserId, created.FullName, created.UserName, created.Role, created.TeamId, created.Team?.Name)));
    }

    [HttpPut("users/{id:int}")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateUser(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(x => x.Team).FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Fail("User not found."));
        }

        user.FullName = request.FullName.Trim();
        user.TeamId = request.TeamId;
        user.Role = request.Role.Trim();
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var updated = await _db.Users.AsNoTracking().Include(x => x.Team).FirstAsync(x => x.UserId == id, cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Success(new UserProfileDto(updated.UserId, updated.FullName, updated.UserName, updated.Role, updated.TeamId, updated.Team?.Name)));
    }

    [HttpDelete("users/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse.Fail("User not found."));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success(new { deleted = true }));
    }

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> GetOrders(
        [FromQuery] string? status,
        [FromQuery] int? roomId,
        [FromQuery] int? teamId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int take = 300,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 1000);

        var query = _db.Orders
            .AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.AssignedTeam)
            .Include(x => x.CreatedByUser)
            .Include(x => x.AcceptedByUser)
            .Include(x => x.Details)
                .ThenInclude(x => x.Item)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (roomId.HasValue)
        {
            query = query.Where(x => x.RoomId == roomId.Value);
        }

        if (teamId.HasValue)
        {
            query = query.Where(x => x.AssignedTeamId == teamId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toUtc.Value);
        }

        var orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<OrderDto>>.Success(orders.Select(MapOrder).ToList()));
    }

    [HttpGet("performance")]
    public async Task<ActionResult<ApiResponse<PerformanceSummaryDto>>> GetPerformance(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-7);

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.AssignedTeam)
            .Include(x => x.AcceptedByUser)
                .ThenInclude(x => x!.Team)
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .ToListAsync(cancellationToken);

        var total = orders.Count;
        var pending = orders.Count(x => x.Status == OrderStatuses.Pending);
        var accepted = orders.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress);
        var completed = orders.Count(x => x.Status == OrderStatuses.Completed);
        var cancelled = orders.Count(x => x.Status == OrderStatuses.Cancelled);
        var escalated = orders.Count(x => x.EscalatedAt.HasValue);

        var byStatus = orders
            .GroupBy(x => x.Status)
            .Select(x => new StatusBreakdownDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var byTeam = orders
            .GroupBy(x => new { x.AssignedTeamId, TeamName = x.AssignedTeam != null ? x.AssignedTeam.Name : "All Teams" })
            .Select(group =>
            {
                var items = group.ToList();
                return new TeamPerformanceDto(
                    group.Key.AssignedTeamId,
                    group.Key.TeamName,
                    items.Count,
                    items.Count(x => x.Status == OrderStatuses.Pending),
                    items.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress),
                    items.Count(x => x.Status == OrderStatuses.Completed),
                    items.Count(x => x.Status == OrderStatuses.Cancelled),
                    items.Count(x => x.EscalatedAt.HasValue),
                    AverageMinutes(items.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)),
                    AverageMinutes(items.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt)));
            })
            .OrderByDescending(x => x.TotalOrders)
            .ToList();

        var byStaff = orders
            .Where(x => x.AcceptedByUserId.HasValue && x.AcceptedByUser != null)
            .GroupBy(x => new
            {
                UserId = x.AcceptedByUserId!.Value,
                x.AcceptedByUser!.FullName,
                TeamName = x.AcceptedByUser.Team != null ? x.AcceptedByUser.Team.Name : null
            })
            .Select(group =>
            {
                var items = group.ToList();
                return new StaffPerformanceDto(
                    group.Key.UserId,
                    group.Key.FullName,
                    group.Key.TeamName,
                    items.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress),
                    items.Count(x => x.Status == OrderStatuses.Completed),
                    AverageMinutes(items.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)),
                    AverageMinutes(items.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt)));
            })
            .OrderByDescending(x => x.CompletedOrders)
            .ThenByDescending(x => x.ActiveOrders)
            .ToList();

        var response = new PerformanceSummaryDto(
            from,
            to,
            total,
            pending,
            accepted,
            completed,
            cancelled,
            escalated,
            total == 0 ? 0 : Math.Round(completed * 100.0 / total, 2),
            AverageMinutes(orders.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)),
            AverageMinutes(orders.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt)),
            byStatus,
            byTeam,
            byStaff);

        return Ok(ApiResponse<PerformanceSummaryDto>.Success(response));
    }

    private static double? AverageMinutes(IEnumerable<TimeSpan> values)
    {
        var list = values.Select(x => x.TotalMinutes).Where(x => x >= 0).ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 2);
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto(
            order.OrderId,
            order.RoomId,
            order.Room.RoomNumber,
            order.AssignedTeamId,
            order.AssignedTeam?.Name,
            order.Source,
            order.Status,
            order.CreatedByUserId,
            order.CreatedByUser?.FullName,
            order.AcceptedByUserId,
            order.AcceptedByUser?.FullName,
            order.CreatedAt,
            order.AcceptedAt,
            order.CompletedAt,
            order.SlaDueAt,
            order.EscalatedAt,
            Convert.ToBase64String(order.RowVersion),
            order.Details.Select(detail => new OrderDetailDto(
                detail.OrderDetailId,
                detail.ItemId,
                detail.Item.Name,
                detail.Quantity,
                detail.DynamicAttributes)).ToList());
    }

}
