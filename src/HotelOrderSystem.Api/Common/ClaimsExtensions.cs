using System.Security.Claims;

namespace HotelOrderSystem.Api.Common;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!int.TryParse(raw, out var userId))
        {
            return 0;
        }
        return userId;
    }

    public static int? GetTeamId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("team_id");
        return int.TryParse(raw, out var teamId) ? teamId : null;
    }

    public static string GetRoleName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
