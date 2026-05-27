namespace HotelOrderSystem.Api.Entities;

public sealed class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int Quantity { get; set; }
    public string DynamicAttributes { get; set; } = "{}";
}
