namespace HotelOrderSystem.Api.Entities;

public sealed class UserDevice
{
    public int UserDeviceId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string DeviceId { get; set; } = string.Empty;
    public string? FcmToken { get; set; }
    public string Platform { get; set; } = "Android";
    public string AppVersion { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
