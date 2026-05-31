namespace HotelOrderSystem.Api.Entities;

public sealed class Item : ISoftDelete
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Product";
    public int ItemCategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public int? TargetTeamId { get; set; }
    public Team? TargetTeam { get; set; }
    public string BaseProperties { get; set; } = "{\"fields\":[]}";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
