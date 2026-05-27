namespace HotelOrderSystem.Api.Config;

public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public int PendingThresholdMinutes { get; set; } = 15;
    public int ScanSeconds { get; set; } = 60;
}
