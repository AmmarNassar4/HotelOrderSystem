namespace HotelOrderSystem.Api.Entities;

public sealed class Room : ISoftDelete
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string DirectLinkPayload { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
