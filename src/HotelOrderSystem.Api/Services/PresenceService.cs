using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelOrderSystem.Api.Services;

public sealed class PresenceService : IPresenceService
{
    private readonly AppDbContext _db;
    private readonly PresenceOptions _options;
    private readonly IRealtimeNotificationService _realtime;

    public PresenceService(AppDbContext db, IOptions<PresenceOptions> options, IRealtimeNotificationService realtime)
    {
        _db = db;
        _options = options.Value;
        _realtime = realtime;
    }

    public async Task<ApiResponse<HeartbeatResponse>> HeartbeatAsync(int userId, HeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var device = await _db.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == request.DeviceId, cancellationToken);
        if (device is not null)
        {
            device.LastSeenAt = now;
            device.UpdatedAt = now;
            device.IsActive = true;
        }

        var presence = await _db.UserPresences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var wasOnline = presence?.IsOnline == true;

        if (presence is null)
        {
            presence = new UserPresence { UserId = userId, IsReady = false };
            _db.UserPresences.Add(presence);
        }

        presence.IsOnline = true;
        presence.LastHeartbeatAt = now;
        presence.LastKnownAppState = request.AppState;
        presence.UpdatedAt = now;

        var user = await _db.Users.AsNoTracking().FirstAsync(x => x.UserId == userId, cancellationToken);
        var canReceivePending = user.Role == Roles.Admin || presence.IsReady;
        var pendingCount = canReceivePending
            ? await _db.Orders.CountAsync(x => x.Status == OrderStatuses.Pending && (user.Role == Roles.Admin || x.AssignedTeamId == null || x.AssignedTeamId == user.TeamId), cancellationToken)
            : 0;

        await _db.SaveChangesAsync(cancellationToken);

        if (!wasOnline)
        {
            await _realtime.NotifyStaffPresenceChangedAsync(userId, true, cancellationToken);
        }

        return ApiResponse<HeartbeatResponse>.Success(new HeartbeatResponse(now, true, presence.IsReady, pendingCount));
    }

    public async Task<ApiResponse<AvailabilityResponse>> SetAvailabilityAsync(int userId, AvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (user is null)
        {
            return ApiResponse<AvailabilityResponse>.Fail("User not found or inactive.");
        }

        if (user.Role == Roles.Admin)
        {
            return ApiResponse<AvailabilityResponse>.Fail("Admin users do not receive staff task availability state.");
        }

        var presence = await _db.UserPresences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var wasReady = presence?.IsReady == true;

        if (presence is null)
        {
            presence = new UserPresence { UserId = userId };
            _db.UserPresences.Add(presence);
        }

        presence.IsOnline = true;
        presence.IsReady = request.IsReady;
        presence.ReadySinceAt = request.IsReady ? (wasReady ? presence.ReadySinceAt ?? now : now) : null;
        presence.LastAvailabilityChangedAt = wasReady == request.IsReady ? presence.LastAvailabilityChangedAt ?? now : now;
        presence.LastHeartbeatAt = now;
        presence.LastKnownAppState = request.Source ?? "availability";
        presence.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(request.DeviceId))
        {
            var device = await _db.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == request.DeviceId, cancellationToken);
            if (device is not null)
            {
                device.LastSeenAt = now;
                device.UpdatedAt = now;
                device.IsActive = true;
            }
        }

        if (wasReady != request.IsReady)
        {
            var openLog = await _db.StaffAvailabilityLogs
                .Where(x => x.UserId == userId && x.EndedAt == null)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (openLog is not null)
            {
                openLog.EndedAt = now;
            }

            _db.StaffAvailabilityLogs.Add(new StaffAvailabilityLog
            {
                UserId = userId,
                IsReady = request.IsReady,
                Source = string.IsNullOrWhiteSpace(request.Source) ? "MobileApp" : request.Source.Trim(),
                DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim(),
                StartedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyStaffPresenceChangedAsync(userId, true, cancellationToken);

        return ApiResponse<AvailabilityResponse>.Success(new AvailabilityResponse(presence.IsReady, presence.ReadySinceAt, presence.LastAvailabilityChangedAt ?? now));
    }

    public async Task<IReadOnlyList<StaffPresenceDto>> GetTeamPresenceAsync(int? teamId, CancellationToken cancellationToken = default)
    {
        var query =
            from user in _db.Users.AsNoTracking()
            join presence in _db.UserPresences.AsNoTracking() on user.UserId equals presence.UserId into presenceGroup
            from presence in presenceGroup.DefaultIfEmpty()
            where user.Role != Roles.Admin && user.IsActive
            select new { user, presence };

        if (teamId.HasValue)
        {
            query = query.Where(x => x.user.TeamId == teamId.Value);
        }

        return await query
            .OrderBy(x => x.user.FullName)
            .Select(x => new StaffPresenceDto(
                x.user.UserId,
                x.user.FullName,
                x.user.TeamId,
                x.user.Team != null ? x.user.Team.Name : null,
                x.presence != null && x.presence.IsOnline,
                x.presence != null && x.presence.IsReady,
                x.presence != null ? x.presence.ReadySinceAt : null,
                x.presence != null ? x.presence.LastHeartbeatAt : null,
                x.presence != null ? x.presence.LastKnownAppState : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CleanupOfflineUsersAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-_options.HeartbeatTimeoutSeconds);
        var stale = await _db.UserPresences
            .Where(x => x.IsOnline && (!x.LastHeartbeatAt.HasValue || x.LastHeartbeatAt < cutoff))
            .ToListAsync(cancellationToken);

        foreach (var presence in stale)
        {
            presence.IsOnline = false;
            presence.UpdatedAt = DateTime.UtcNow;
            await _realtime.NotifyStaffPresenceChangedAsync(presence.UserId, false, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }
}
