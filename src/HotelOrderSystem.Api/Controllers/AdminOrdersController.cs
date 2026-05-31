using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/orders")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminOrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
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

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (roomId.HasValue) query = query.Where(x => x.RoomId == roomId.Value);
        if (teamId.HasValue) query = query.Where(x => x.AssignedTeamId == teamId.Value);
        if (fromUtc.HasValue) query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.CreatedAt <= toUtc.Value);

        var orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<OrderDto>>.Success(orders.Select(OrderMapper.MapToDto).ToList()));
    }
}
