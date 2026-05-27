using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Entities;

namespace HotelOrderSystem.Api.Services;

public sealed class NotificationOutboxService : INotificationOutboxService
{
    private readonly AppDbContext _db;

    public NotificationOutboxService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(string type, int? targetUserId, int? targetTeamId, string payloadJson, CancellationToken cancellationToken = default)
    {
        if (!JsonValidator.IsValidObjectJson(payloadJson))
        {
            payloadJson = "{}";
        }

        _db.NotificationOutbox.Add(new NotificationOutbox
        {
            Type = type,
            TargetUserId = targetUserId,
            TargetTeamId = targetTeamId,
            PayloadJson = payloadJson,
            Status = NotificationStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
