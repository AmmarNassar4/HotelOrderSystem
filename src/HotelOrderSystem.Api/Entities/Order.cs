using System.ComponentModel.DataAnnotations;

namespace HotelOrderSystem.Api.Entities;

public sealed class Order
{
    public int OrderId { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public int? AssignedTeamId { get; set; }
    public Team? AssignedTeam { get; set; }

    public string Source { get; set; } = "Admin";
    public string Status { get; set; } = "Pending";

    public int? AcceptedByUserId { get; set; }
    public User? AcceptedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public DateTime? EscalatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
}
