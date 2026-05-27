namespace HotelOrderSystem.Api.Config;

public sealed class PresenceOptions
{
    public const string SectionName = "Presence";

    public int HeartbeatTimeoutSeconds { get; set; } = 120;
    public int CleanupSeconds { get; set; } = 30;
}
