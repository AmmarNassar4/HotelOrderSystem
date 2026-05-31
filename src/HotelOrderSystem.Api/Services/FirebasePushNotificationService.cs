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
        if (_options.FcmMode == NotificationOptions.FcmModeStub)
        {
            _logger.LogInformation("FCM stub mode enabled: type={Type}, tokenCount={TokenCount}", type, tokens.Count);
            return;
        }

        if (tokens.Count == 0)
        {
            _logger.LogInformation("FCM skipped: type={Type}, tokenCount=0", type);
            return;
        }

        var sender = FirebaseMessaging.DefaultInstance;
        if (sender == null)
        {
            _logger.LogWarning("Firebase not initialized. Skipping FCM push for {TokenCount} token(s).", tokens.Count);
            return;
        }

        var body = GetBodyFromPayload(payloadJson);

        using var semaphore = new SemaphoreSlim(10);
        var tasks = new List<Task<FcmSendResult>>();
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

            tasks.Add(SendThrottledAsync(sender, semaphore, token, message));
        }

        var results = await Task.WhenAll(tasks);

        var success = results.Count(x => x.Success);
        var failure = results.Length - success;
        var invalidTokens = results
            .Where(x => !x.Success && x.ShouldDeactivateToken)
            .Select(x => x.Token)
            .ToList();

        foreach (var result in results.Where(x => !x.Success))
        {
            _logger.LogWarning(
                "FCM send failed: tokenPrefix={TokenPrefix}, shouldDeactivate={ShouldDeactivate}, error={Error}",
                GetTokenPrefix(result.Token),
                result.ShouldDeactivateToken,
                result.Error);
        }

        _logger.LogInformation("FCM sent: Success={Success}, Failure={Failure}, Total={Total}", success, failure, tokens.Count);

        if (invalidTokens.Count > 0)
        {
            await _db.UserDevices
                .Where(x => invalidTokens.Contains(x.FcmToken!))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), cancellationToken);
            _logger.LogWarning("Deactivated {Count} invalid or unregistered FCM tokens.", invalidTokens.Count);
        }
    }

    private static async Task<FcmSendResult> SendThrottledAsync(FirebaseMessaging sender, SemaphoreSlim semaphore, string token, Message message)
    {
        await semaphore.WaitAsync();
        try
        {
            return await SendSingleAsync(sender, token, message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<FcmSendResult> SendSingleAsync(FirebaseMessaging sender, string token, Message message)
    {
        try
        {
            await sender.SendAsync(message);
            return FcmSendResult.Ok(token);
        }
        catch (FirebaseMessagingException ex)
        {
            return FcmSendResult.Fail(token, GetFirebaseExceptionDetails(ex), ShouldDeactivateToken(ex));
        }
        catch (Exception ex)
        {
            return FcmSendResult.Fail(token, $"{ex.GetType().Name}: {ex.Message}", false);
        }
    }

    private static bool ShouldDeactivateToken(FirebaseMessagingException ex)
    {
        var details = GetFirebaseExceptionDetails(ex);
        return details.Contains("unregistered", StringComparison.OrdinalIgnoreCase)
            || details.Contains("registration-token-not-registered", StringComparison.OrdinalIgnoreCase)
            || details.Contains("invalid-registration-token", StringComparison.OrdinalIgnoreCase)
            || details.Contains("not a valid FCM registration token", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFirebaseExceptionDetails(FirebaseMessagingException ex)
    {
        var errorCode = ex.GetType().GetProperty("ErrorCode")?.GetValue(ex)?.ToString();
        var messagingErrorCode = ex.GetType().GetProperty("MessagingErrorCode")?.GetValue(ex)?.ToString();

        return $"{ex.GetType().Name}: ErrorCode={errorCode ?? "unknown"}; MessagingErrorCode={messagingErrorCode ?? "unknown"}; Message={ex.Message}";
    }

    private static string GetTokenPrefix(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "empty";
        return token.Length <= 12 ? token : token[..12];
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

    private sealed record FcmSendResult(string Token, bool Success, bool ShouldDeactivateToken, string? Error)
    {
        public static FcmSendResult Ok(string token) => new(token, true, false, null);

        public static FcmSendResult Fail(string token, string error, bool shouldDeactivateToken) => new(token, false, shouldDeactivateToken, error);
    }
}
