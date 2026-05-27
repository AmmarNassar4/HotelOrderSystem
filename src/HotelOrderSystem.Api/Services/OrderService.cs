using System.Text.Json;
using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelOrderSystem.Api.Services;

public sealed class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly SlaOptions _slaOptions;
    private readonly IRealtimeNotificationService _realtime;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext db,
        IOptions<SlaOptions> slaOptions,
        IRealtimeNotificationService realtime,
        ILogger<OrderService> logger)
    {
        _db = db;
        _slaOptions = slaOptions.Value;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<ApiResponse<CreateOrderResponse>> CreateAsync(CreateOrderRequest request, int? createdByUserId, string role, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            return ApiResponse<CreateOrderResponse>.Fail("Order must contain at least one item.");
        }

        var roomExists = await _db.Rooms.AnyAsync(x => x.RoomId == request.RoomId && x.IsActive, cancellationToken);
        if (!roomExists)
        {
            return ApiResponse<CreateOrderResponse>.Fail("Room not found or inactive.");
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            return ApiResponse<CreateOrderResponse>.Fail("Quantity must be greater than zero.");
        }

        var itemIds = request.Items.Select(x => x.ItemId).Distinct().ToList();
        var items = await _db.Items
            .Where(x => itemIds.Contains(x.ItemId) && x.IsActive)
            .ToDictionaryAsync(x => x.ItemId, cancellationToken);

        if (items.Count != itemIds.Count)
        {
            return ApiResponse<CreateOrderResponse>.Fail("One or more items were not found or inactive.");
        }

        foreach (var line in request.Items)
        {
            var attributes = line.DynamicAttributes.HasValue ? line.DynamicAttributes.Value.GetRawText() : "{}";
            if (!JsonValidator.IsValidObjectJson(attributes))
            {
                return ApiResponse<CreateOrderResponse>.Fail($"Dynamic attributes for item {line.ItemId} must be a valid JSON object.");
            }
        }

        var now = DateTime.UtcNow;
        var source = string.IsNullOrWhiteSpace(request.Source)
            ? role == Roles.Admin ? OrderSources.Admin : OrderSources.StaffProxy
            : request.Source;

        var orders = request.Items
            .GroupBy(line => items[line.ItemId].TargetTeamId)
            .Select(group => new Order
            {
                RoomId = request.RoomId,
                CreatedByUserId = createdByUserId,
                AssignedTeamId = group.Key,
                Source = source,
                Status = OrderStatuses.Pending,
                CreatedAt = now,
                SlaDueAt = now.AddMinutes(_slaOptions.PendingThresholdMinutes),
                Details = group.Select(line => new OrderDetail
                {
                    ItemId = line.ItemId,
                    Quantity = line.Quantity,
                    DynamicAttributes = line.DynamicAttributes.HasValue ? line.DynamicAttributes.Value.GetRawText() : "{}"
                }).ToList()
            })
            .ToList();

        foreach (var order in orders)
        {
            _db.Orders.Add(order);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var createdIds = orders.Select(x => x.OrderId).ToList();
        var createdOrders = await QueryOrders()
            .Where(x => createdIds.Contains(x.OrderId))
            .OrderBy(x => x.OrderId)
            .ToListAsync(cancellationToken);

        var responseOrders = createdOrders.Select(MapOrder).ToList();

        foreach (var order in responseOrders)
        {
            EnqueueNotification(NotificationTypes.OrderCreated, null, order.AssignedTeamId, JsonSerializer.Serialize(new
            {
                type = NotificationTypes.OrderCreated,
                orderId = order.OrderId,
                roomId = order.RoomId,
                roomNumber = order.RoomNumber,
                assignedTeamId = order.AssignedTeamId,
                createdAtUtc = order.CreatedAtUtc
            }));

            await _realtime.NotifyOrderCreatedAsync(order, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);

        return ApiResponse<CreateOrderResponse>.Success(new CreateOrderResponse(responseOrders));
    }

    public async Task<ApiResponse<IReadOnlyList<OrderDto>>> GetPendingAsync(int userId, int? teamId, string role, CancellationToken cancellationToken = default)
    {
        if (role != Roles.Admin && !await IsReadyAsync(userId, cancellationToken))
        {
            return ApiResponse<IReadOnlyList<OrderDto>>.Success(Array.Empty<OrderDto>());
        }

        var query = QueryOrders()
            .Where(x => x.Status == OrderStatuses.Pending);

        if (role != Roles.Admin)
        {
            query = query.Where(x => x.AssignedTeamId == null || x.AssignedTeamId == teamId);
        }

        var orders = await query
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<OrderDto>>.Success(orders.Select(MapOrder).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<OrderDto>>> GetMyActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var orders = await QueryOrders()
            .Where(x => x.AcceptedByUserId == userId && (x.Status == OrderStatuses.Accepted || x.Status == OrderStatuses.InProgress))
            .OrderBy(x => x.AcceptedAt)
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<OrderDto>>.Success(orders.Select(MapOrder).ToList());
    }

    public async Task<ApiResponse<OrderDto>> GetByIdAsync(int orderId, int userId, int? teamId, string role, CancellationToken cancellationToken = default)
    {
        var order = await QueryOrders().FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return ApiResponse<OrderDto>.Fail("Order not found.");
        }

        if (!CanAccessOrder(order, userId, teamId, role))
        {
            return ApiResponse<OrderDto>.Fail("You do not have permission to view this order.");
        }

        return ApiResponse<OrderDto>.Success(MapOrder(order));
    }

    public async Task<ApiResponse<OrderDto>> AcceptAsync(int orderId, int userId, int? teamId, string role, AcceptOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (role != Roles.Admin && !await IsReadyAsync(userId, cancellationToken))
        {
            return ApiResponse<OrderDto>.Fail("Set your status to Ready before accepting new orders.");
        }

        var order = await QueryOrders().FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return ApiResponse<OrderDto>.Fail("Order not found.");
        }

        if (!CanAccessTeam(order.AssignedTeamId, teamId, role))
        {
            return ApiResponse<OrderDto>.Fail("You do not have permission to accept this order.");
        }

        if (order.Status != OrderStatuses.Pending)
        {
            return ApiResponse<OrderDto>.Fail("Order has already been accepted or closed.");
        }

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var expected = Convert.FromBase64String(request.RowVersion);
            if (!order.RowVersion.SequenceEqual(expected))
            {
                return ApiResponse<OrderDto>.Fail("Order was changed by another user. Please refresh.");
            }
        }

        order.Status = OrderStatuses.Accepted;
        order.AcceptedByUserId = userId;
        order.AcceptedAt = DateTime.UtcNow;

        EnqueueNotification(NotificationTypes.OrderClaimed, null, order.AssignedTeamId, JsonSerializer.Serialize(new
        {
            type = NotificationTypes.OrderClaimed,
            orderId = order.OrderId,
            acceptedByUserId = userId,
            assignedTeamId = order.AssignedTeamId
        }));

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while accepting order {OrderId}", orderId);
            return ApiResponse<OrderDto>.Fail("Order was already accepted by another staff member.");
        }

        var updated = await QueryOrders().FirstAsync(x => x.OrderId == orderId, cancellationToken);
        var dto = MapOrder(updated);
        await _realtime.NotifyOrderAcceptedAsync(dto, cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);

        return ApiResponse<OrderDto>.Success(dto);
    }

    public async Task<ApiResponse<OrderDto>> CompleteAsync(int orderId, int userId, string role, CompleteOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await QueryOrders().FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return ApiResponse<OrderDto>.Fail("Order not found.");
        }

        if (order.Status != OrderStatuses.Accepted && order.Status != OrderStatuses.InProgress)
        {
            return ApiResponse<OrderDto>.Fail("Only accepted or in-progress orders can be completed.");
        }

        if (role != Roles.Admin && order.AcceptedByUserId != userId)
        {
            return ApiResponse<OrderDto>.Fail("Only the accepting staff member can complete this order.");
        }

        order.Status = OrderStatuses.Completed;
        order.CompletedAt = DateTime.UtcNow;

        EnqueueNotification(NotificationTypes.OrderCompleted, null, order.AssignedTeamId, JsonSerializer.Serialize(new
        {
            type = NotificationTypes.OrderCompleted,
            orderId = order.OrderId,
            completedByUserId = userId,
            completedAtUtc = order.CompletedAt
        }));

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await QueryOrders().FirstAsync(x => x.OrderId == orderId, cancellationToken);
        var dto = MapOrder(updated);
        await _realtime.NotifyOrderCompletedAsync(dto, cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);

        return ApiResponse<OrderDto>.Success(dto);
    }

    public async Task<ApiResponse<OrderDto>> CancelAsync(int orderId, int userId, string role, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (role != Roles.Admin && role != Roles.Supervisor)
        {
            return ApiResponse<OrderDto>.Fail("Only admins or supervisors can cancel orders.");
        }

        var order = await QueryOrders().FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return ApiResponse<OrderDto>.Fail("Order not found.");
        }

        if (order.Status == OrderStatuses.Completed || order.Status == OrderStatuses.Cancelled)
        {
            return ApiResponse<OrderDto>.Fail("Order is already closed.");
        }

        order.Status = OrderStatuses.Cancelled;
        order.CancelledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        var dto = MapOrder(order);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
        return ApiResponse<OrderDto>.Success(dto);
    }

    private IQueryable<Order> QueryOrders()
    {
        return _db.Orders
            .Include(x => x.Room)
            .Include(x => x.AssignedTeam)
            .Include(x => x.CreatedByUser)
            .Include(x => x.AcceptedByUser)
            .Include(x => x.Details)
                .ThenInclude(x => x.Item)
            .AsSplitQuery();
    }

    private void EnqueueNotification(string type, int? targetUserId, int? targetTeamId, string payloadJson)
    {
        if (!JsonValidator.IsValidObjectJson(payloadJson))
        {
            payloadJson = "{}";
        }

        _db.NotificationOutbox.Add(new NotificationOutbox
        {
            Type = type,
            TargetUserId = targetUserId,
            TargetTeamId = targetTeamId,
            PayloadJson = payloadJson,
            Status = NotificationStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    private Task<bool> IsReadyAsync(int userId, CancellationToken cancellationToken)
    {
        return _db.UserPresences.AnyAsync(x => x.UserId == userId && x.IsReady, cancellationToken);
    }

    private static bool CanAccessTeam(int? orderTeamId, int? userTeamId, string role)
    {
        if (role == Roles.Admin)
        {
            return true;
        }

        if (!orderTeamId.HasValue)
        {
            return true;
        }

        return userTeamId.HasValue && userTeamId.Value == orderTeamId.Value;
    }

    private static bool CanAccessOrder(Order order, int userId, int? teamId, string role)
    {
        if (role == Roles.Admin)
        {
            return true;
        }

        if (order.AcceptedByUserId == userId)
        {
            return true;
        }

        return CanAccessTeam(order.AssignedTeamId, teamId, role);
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto(
            order.OrderId,
            order.RoomId,
            order.Room.RoomNumber,
            order.AssignedTeamId,
            order.AssignedTeam?.Name,
            order.Source,
            order.Status,
            order.CreatedByUserId,
            order.CreatedByUser?.FullName,
            order.AcceptedByUserId,
            order.AcceptedByUser?.FullName,
            order.CreatedAt,
            order.AcceptedAt,
            order.CompletedAt,
            order.SlaDueAt,
            order.EscalatedAt,
            order.RowVersion.Length == 0 ? string.Empty : Convert.ToBase64String(order.RowVersion),
            order.Details.Select(detail => new OrderDetailDto(
                detail.OrderDetailId,
                detail.ItemId,
                detail.Item.Name,
                detail.Quantity,
                detail.DynamicAttributes)).ToList());
    }
}
