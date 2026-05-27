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
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(AppDbContext db, IPasswordService passwordService, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(x => x.Team)
            .FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive, cancellationToken);

        if (user is null || !_passwordService.VerifyPassword(user, request.Password))
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid username or password."));
        }

        var token = _jwtTokenService.CreateToken(user);
        return Ok(ApiResponse<AuthResponse>.Success(token));
    }

    [Authorize]
    [HttpPut("device-token")]
    public async Task<ActionResult<ApiResponse>> RegisterDeviceToken(DeviceTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId <= 0)
        {
            return Unauthorized(ApiResponse.Fail("Invalid token."));
        }

        var now = DateTime.UtcNow;
        var device = await _db.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == request.DeviceId, cancellationToken);

        if (device is null)
        {
            device = new UserDevice
            {
                UserId = userId,
                DeviceId = request.DeviceId,
                CreatedAt = now
            };
            _db.UserDevices.Add(device);
        }

        device.FcmToken = request.FcmToken;
        device.Platform = request.Platform;
        device.AppVersion = request.AppVersion;
        device.LastSeenAt = now;
        device.UpdatedAt = now;
        device.IsActive = true;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success(new { registered = true }));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var device = await _db.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == request.DeviceId, cancellationToken);
        if (device is not null)
        {
            device.IsActive = false;
            device.FcmToken = null;
            device.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse.Success(new { loggedOut = true }));
    }
}
