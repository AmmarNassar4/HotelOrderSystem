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
[Route("api/v1/admin/teams")]
public sealed class AdminTeamsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICatalogService _catalog;

    public AdminTeamsController(AppDbContext db, ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TeamDto>>>> GetTeams(CancellationToken cancellationToken)
    {
        var rows = await _db.Teams
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TeamDto(x.TeamId, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<TeamDto>>.Success(rows));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TeamDto>>> CreateTeam(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = new Team { Name = request.Name.Trim(), IsActive = request.IsActive };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(cancellationToken);
        _catalog.ClearCache();

        return Ok(ApiResponse<TeamDto>.Success(new TeamDto(team.TeamId, team.Name, team.IsActive)));
    }

    [HttpPut("{id:int}")]
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

    [HttpDelete("{id:int}")]
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
}
