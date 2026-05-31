namespace HotelOrderSystem.Api.Config;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public const string FcmModeStub = "Stub";

    public int OutboxScanSeconds { get; set; } = 10;
    public string FcmMode { get; set; } = FcmModeStub;
    public string FirebaseProjectId { get; set; } = string.Empty;
}
