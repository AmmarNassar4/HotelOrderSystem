namespace HotelOrderSystem.Api.Entities;

public sealed class StaffAvailabilityLog
{
    public int StaffAvailabilityLogId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsReady { get; set; }
    public string Source { get; set; } = "MobileApp";
    public string? DeviceId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}
