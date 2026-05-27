using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserEntity = HotelOrderSystem.Api.Entities.User;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwordService;

    public AdminUsersController(AppDbContext db, IPasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserProfileDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var rows = await _db.Users
            .AsNoTracking()
            .Include(x => x.Team)
            .OrderBy(x => x.FullName)
            .Select(x => new UserProfileDto(x.UserId, x.FullName, x.UserName, x.Role, x.TeamId, x.Team != null ? x.Team.Name : null))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<UserProfileDto>>.Success(rows));
    }

    [HttpPost]
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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateUser(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
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

    [HttpDelete("{id:int}")]
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
}
