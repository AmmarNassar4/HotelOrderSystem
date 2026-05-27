using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/presence")]
public sealed class PresenceController : ControllerBase
{
    private readonly IPresenceService _presence;

    public PresenceController(IPresenceService presence)
    {
        _presence = presence;
    }

    [HttpPut("heartbeat")]
    public async Task<ActionResult<ApiResponse<HeartbeatResponse>>> Heartbeat(HeartbeatRequest request, CancellationToken cancellationToken)
    {
        var result = await _presence.HeartbeatAsync(User.GetUserId(), request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("availability")]
    public async Task<ActionResult<ApiResponse<AvailabilityResponse>>> SetAvailability(AvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _presence.SetAvailabilityAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Supervisor)]
    [HttpGet("team/{teamId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StaffPresenceDto>>>> GetTeamPresence(int teamId, CancellationToken cancellationToken)
    {
        var result = await _presence.GetTeamPresenceAsync(teamId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StaffPresenceDto>>.Success(result));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("users/online")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StaffPresenceDto>>>> GetAllPresence(CancellationToken cancellationToken)
    {
        var result = await _presence.GetTeamPresenceAsync(null, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StaffPresenceDto>>.Success(result));
    }
}
