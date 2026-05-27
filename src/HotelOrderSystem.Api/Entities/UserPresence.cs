namespace HotelOrderSystem.Api.Entities;

public sealed class UserPresence
{
    public int UserPresenceId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsOnline { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public string? LastConnectionId { get; set; }
    public string? LastKnownAppState { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
