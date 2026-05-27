namespace HotelOrderSystem.Api.Entities;

public sealed class ItemCategory : ISoftDelete
{
    public int ItemCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
