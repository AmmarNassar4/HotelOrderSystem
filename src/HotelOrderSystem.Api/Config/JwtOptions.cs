namespace HotelOrderSystem.Api.Config;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "HotelOrderSystem";
    public string Audience { get; set; } = "HotelOrderSystemClients";
    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_LONG_SECURE_RANDOM_KEY_32_CHARS_MINIMUM";
    public int AccessTokenMinutes { get; set; } = 120;
}
