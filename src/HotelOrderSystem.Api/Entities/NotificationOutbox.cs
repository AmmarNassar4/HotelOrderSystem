namespace HotelOrderSystem.Api.Entities;

public sealed class NotificationOutbox
{
    public long NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? TargetUserId { get; set; }
    public int? TargetTeamId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
