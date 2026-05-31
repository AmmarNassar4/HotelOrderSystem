using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelOrderSystem.Api.Workers;

public sealed class NotificationOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationOutboxWorker> _logger;
    private readonly NotificationOptions _options;

    public NotificationOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationOutboxWorker> logger, IOptions<NotificationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.OutboxScanSeconds));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification outbox processing failed; will retry next interval.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var pending = await db.NotificationOutbox
            .Where(x => x.Status == NotificationStatuses.Pending && x.RetryCount < 5)
            .OrderBy(x => x.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var notification in pending)
        {
            try
            {
                if (notification.TargetUserId.HasValue)
                {
                    await push.SendToUserAsync(notification.TargetUserId.Value, notification.Type, notification.PayloadJson, cancellationToken);
                }
                else if (notification.TargetTeamId.HasValue)
                {
                    await push.SendToTeamAsync(notification.TargetTeamId.Value, notification.Type, notification.PayloadJson, cancellationToken);
                }
                else if (notification.Type is NotificationTypes.OrderCreated or NotificationTypes.OrderClaimed or NotificationTypes.OrderCancelled)
                {
                    await push.SendBroadcastAsync(notification.Type, notification.PayloadJson, cancellationToken);
                }
                else
                {
                    await push.SendToAdminsAsync(notification.Type, notification.PayloadJson, cancellationToken);
                }

                notification.Status = NotificationStatuses.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.LastError = null;
            }
            catch (Exception ex)
            {
                notification.RetryCount += 1;
                notification.Status = notification.RetryCount >= 5 ? NotificationStatuses.Failed : NotificationStatuses.Pending;
                notification.LastError = ex.Message;
                _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.NotificationId);
            }
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
