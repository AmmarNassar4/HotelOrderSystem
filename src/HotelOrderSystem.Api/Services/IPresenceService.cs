using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Dtos;

namespace HotelOrderSystem.Api.Services;

public interface IPresenceService
{
    Task<ApiResponse<HeartbeatResponse>> HeartbeatAsync(int userId, HeartbeatRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AvailabilityResponse>> SetAvailabilityAsync(int userId, AvailabilityRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffPresenceDto>> GetTeamPresenceAsync(int? teamId, CancellationToken cancellationToken = default);
    Task<int> CleanupOfflineUsersAsync(CancellationToken cancellationToken = default);
}
