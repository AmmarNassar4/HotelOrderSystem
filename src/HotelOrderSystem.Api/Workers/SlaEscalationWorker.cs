using System.Text.Json;
using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Entities;
using HotelOrderSystem.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelOrderSystem.Api.Workers;

public sealed class SlaEscalationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaEscalationWorker> _logger;
    private readonly SlaOptions _options;

    public SlaEscalationWorker(IServiceScopeFactory scopeFactory, ILogger<SlaEscalationWorker> logger, IOptions<SlaOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.ScanSeconds));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await EscalatePendingOrdersAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task EscalatePendingOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var realtime = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        var now = DateTime.UtcNow;

        var overdue = await db.Orders
            .Include(x => x.Room)
            .Where(x => x.Status == OrderStatuses.Pending && x.SlaDueAt != null && x.SlaDueAt <= now && x.EscalatedAt == null)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var order in overdue)
        {
            order.EscalatedAt = now;
            db.NotificationOutbox.Add(new NotificationOutbox
            {
                Type = NotificationTypes.SlaEscalated,
                TargetTeamId = order.AssignedTeamId,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    type = NotificationTypes.SlaEscalated,
                    orderId = order.OrderId,
                    roomId = order.RoomId,
                    roomNumber = order.Room.RoomNumber,
                    assignedTeamId = order.AssignedTeamId,
                    escalatedAtUtc = now
                }),
                Status = NotificationStatuses.Pending,
                CreatedAt = now
            });
        }

        if (overdue.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await realtime.NotifyDashboardChangedAsync(cancellationToken);
            _logger.LogInformation("Escalated {Count} overdue orders.", overdue.Count);
        }
    }
}
