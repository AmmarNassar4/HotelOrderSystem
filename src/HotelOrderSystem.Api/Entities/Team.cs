namespace HotelOrderSystem.Api.Entities;

public sealed class Team : ISoftDelete
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
