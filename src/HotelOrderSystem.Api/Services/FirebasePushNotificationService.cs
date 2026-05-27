using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Services;

public sealed class FirebasePushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FirebasePushNotificationService> _logger;

    public FirebasePushNotificationService(AppDbContext db, ILogger<FirebasePushNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendToUserAsync(int userId, string type, string payloadJson, CancellationToken cancellationToken = default)
    {
        var tokens = await _db.UserDevices
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.FcmToken != null)
            .Select(x => x.FcmToken!)
            .ToListAsync(cancellationToken);

        await SendTokensAsync(tokens, type, payloadJson, cancellationToken);
    }

    public async Task SendToTeamAsync(int teamId, string type, string payloadJson, CancellationToken cancellationToken = default)
    {
        var tokens = await _db.UserDevices
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.FcmToken != null &&
                x.User.TeamId == teamId &&
                x.User.IsActive &&
                x.User.Role != Roles.Admin &&
                _db.UserPresences.Any(p => p.UserId == x.UserId && p.IsReady))
            .Select(x => x.FcmToken!)
            .Distinct()
            .ToListAsync(cancellationToken);

        await SendTokensAsync(tokens, type, payloadJson, cancellationToken);
    }

    public async Task SendToAdminsAsync(string type, string payloadJson, CancellationToken cancellationToken = default)
    {
        var tokens = await _db.UserDevices
            .AsNoTracking()
            .Where(x => x.IsActive && x.FcmToken != null && x.User.Role == Roles.Admin && x.User.IsActive)
            .Select(x => x.FcmToken!)
            .Distinct()
            .ToListAsync(cancellationToken);

        await SendTokensAsync(tokens, type, payloadJson, cancellationToken);
    }

    public async Task SendBroadcastAsync(string type, string payloadJson, CancellationToken cancellationToken = default)
    {
        var tokens = await _db.UserDevices
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.FcmToken != null &&
                x.User.IsActive &&
                (x.User.Role == Roles.Admin || _db.UserPresences.Any(p => p.UserId == x.UserId && p.IsReady)))
            .Select(x => x.FcmToken!)
            .Distinct()
            .ToListAsync(cancellationToken);

        await SendTokensAsync(tokens, type, payloadJson, cancellationToken);
    }

    private Task SendTokensAsync(IReadOnlyList<string> tokens, string type, string payloadJson, CancellationToken cancellationToken)
    {
        // Production TODO:
        // 1. Add Firebase Admin SDK or HTTP v1 sender.
        // 2. Send a data message with keys: type, payload.
        // 3. Mark invalid tokens inactive when FCM returns NotRegistered/InvalidRegistration.
        _logger.LogInformation("FCM stub: type={Type}, tokenCount={TokenCount}, payload={Payload}", type, tokens.Count, payloadJson);
        return Task.CompletedTask;
    }
}
