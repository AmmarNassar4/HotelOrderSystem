using System.Text.Json;

namespace HotelOrderSystem.Api.Common;

public static class JsonValidator
{
    public static bool IsValidObjectJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string NormalizeObjectJson(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? "{}" : json;
    }
}
