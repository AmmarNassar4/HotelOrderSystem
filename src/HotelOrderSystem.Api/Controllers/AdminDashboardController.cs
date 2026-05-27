using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminDashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard/live-summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var summary = new DashboardSummaryDto(
            await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Pending, cancellationToken),
            await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress, cancellationToken),
            await _db.UserPresences.CountAsync(x => x.IsOnline, cancellationToken),
            await _db.Rooms.CountAsync(x => x.IsActive, cancellationToken),
            DateTime.UtcNow);

        return Ok(ApiResponse<DashboardSummaryDto>.Success(summary));
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
        var completed = orders.Count(x => x.Status == OrderStatuses.Completed);
        var byStatus = orders
            .GroupBy(x => x.Status)
            .Select(x => new StatusBreakdownDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var byTeam = orders
            .GroupBy(x => new { x.AssignedTeamId, TeamName = x.AssignedTeam != null ? x.AssignedTeam.Name : "All Teams" })
            .Select(g => new TeamPerformanceDto(
                g.Key.AssignedTeamId,
                g.Key.TeamName,
                g.Count(),
                g.Count(x => x.Status == OrderStatuses.Pending),
                g.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress),
                g.Count(x => x.Status == OrderStatuses.Completed),
                g.Count(x => x.Status == OrderStatuses.Cancelled),
                g.Count(x => x.EscalatedAt.HasValue),
                AverageMinutes(g.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)),
                AverageMinutes(g.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt))))
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
            .Select(g => new StaffPerformanceDto(
                g.Key.UserId,
                g.Key.FullName,
                g.Key.TeamName,
                g.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress),
                g.Count(x => x.Status == OrderStatuses.Completed),
                AverageMinutes(g.Where(x => x.AcceptedAt.HasValue).Select(x => x.AcceptedAt!.Value - x.CreatedAt)),
                AverageMinutes(g.Where(x => x.CompletedAt.HasValue).Select(x => x.CompletedAt!.Value - x.CreatedAt))))
            .OrderByDescending(x => x.CompletedOrders)
            .ToList();

        var response = new PerformanceSummaryDto(
            from,
            to,
            total,
            orders.Count(x => x.Status == OrderStatuses.Pending),
            orders.Count(x => x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress),
            completed,
            orders.Count(x => x.Status == OrderStatuses.Cancelled),
            orders.Count(x => x.EscalatedAt.HasValue),
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
}
