using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Services;
using Microsoft.Extensions.Options;

namespace HotelOrderSystem.Api.Workers;

public sealed class PresenceCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PresenceOptions _options;
    private readonly ILogger<PresenceCleanupWorker> _logger;

    public PresenceCleanupWorker(IServiceScopeFactory scopeFactory, IOptions<PresenceOptions> options, ILogger<PresenceCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.CleanupSeconds));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var presence = scope.ServiceProvider.GetRequiredService<IPresenceService>();
            var count = await presence.CleanupOfflineUsersAsync(stoppingToken);
            if (count > 0)
            {
                _logger.LogInformation("Marked {Count} users offline.", count);
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
