using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Dtos;

namespace HotelOrderSystem.Api.Services;

public interface IOrderService
{
    Task<ApiResponse<CreateOrderResponse>> CreateAsync(CreateOrderRequest request, int? createdByUserId, string role, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<OrderDto>>> GetPendingAsync(int userId, int? teamId, string role, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<OrderDto>>> GetMyActiveAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDto>> GetByIdAsync(int orderId, int userId, int? teamId, string role, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDto>> AcceptAsync(int orderId, int userId, int? teamId, string role, AcceptOrderRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDto>> CompleteAsync(int orderId, int userId, string role, CompleteOrderRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDto>> CancelAsync(int orderId, int userId, string role, CancelOrderRequest request, CancellationToken cancellationToken = default);
}
