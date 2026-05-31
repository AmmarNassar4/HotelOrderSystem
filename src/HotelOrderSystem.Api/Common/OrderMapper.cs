using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Entities;

namespace HotelOrderSystem.Api.Common;

public static class OrderMapper
{
    public static OrderDto MapToDto(Order order)
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
