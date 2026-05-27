namespace HotelOrderSystem.Api.Entities;

public sealed class User : ISoftDelete
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int? TeamId { get; set; }
    public Team? Team { get; set; }
    public string Role { get; set; } = "Staff";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
}
