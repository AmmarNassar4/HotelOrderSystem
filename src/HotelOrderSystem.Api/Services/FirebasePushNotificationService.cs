using System.Text.Json;
using FirebaseAdmin.Messaging;
using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelOrderSystem.Api.Services;

public sealed class FirebasePushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FirebasePushNotificationService> _logger;
    private readonly NotificationOptions _options;

    public FirebasePushNotificationService(AppDbContext db, ILogger<FirebasePushNotificationService> logger, IOptions<NotificationOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options.Value;
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

    private async Task SendTokensAsync(IReadOnlyList<string> tokens, string type, string payloadJson, CancellationToken cancellationToken)
    {
        if (_options.FcmMode == "Stub" || tokens.Count == 0)
        {
            _logger.LogInformation("FCM stub: type={Type}, tokenCount={TokenCount}", type, tokens.Count);
            return;
        }

        var sender = FirebaseMessaging.DefaultInstance;
        if (sender == null)
        {
            _logger.LogWarning("Firebase not initialized (service account JSON missing). Skipping FCM push for {TokenCount} token(s).", tokens.Count);
            return;
        }

        var body = GetBodyFromPayload(payloadJson);

        var tasks = new List<Task<(bool Success, string Token)>>();
        foreach (var token in tokens)
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = "Hotel Order",
                    Body = body
                },
                Data = new Dictionary<string, string>
                {
                    { "type", type },
                    { "payload", payloadJson }
                }
            };

            tasks.Add(SendSingleAsync(sender, token, message));
        }

        var results = await Task.WhenAll(tasks);

        int success = 0, failure = 0;
        var invalidTokens = new List<string>();
        foreach (var (isSuccess, tok) in results)
        {
            if (isSuccess)
            {
                success++;
            }
            else
            {
                failure++;
                invalidTokens.Add(tok);
            }
        }

        _logger.LogInformation("FCM sent: Success={Success}, Failure={Failure}, Total={Total}", success, failure, tokens.Count);

        if (invalidTokens.Count > 0)
        {
            await _db.UserDevices
                .Where(x => invalidTokens.Contains(x.FcmToken!))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), cancellationToken);
            _logger.LogWarning("Deactivated {Count} invalid FCM tokens.", invalidTokens.Count);
        }
    }

    private static async Task<(bool Success, string Token)> SendSingleAsync(FirebaseMessaging sender, string token, Message message)
    {
        try
        {
            await sender.SendAsync(message);
            return (true, token);
        }
        catch (FirebaseMessagingException)
        {
            return (false, token);
        }
    }

    private static string GetBodyFromPayload(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("roomNumber", out var room))
            {
                return $"New order from Room {room.GetString()}";
            }
            if (root.TryGetProperty("orderId", out var orderId))
            {
                return $"Order #{orderId.GetInt32()} needs attention";
            }
        }
        catch
        {
            // Fall through to generic message
        }
        return "You have a new notification";
    }
}
