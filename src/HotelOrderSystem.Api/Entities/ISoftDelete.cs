namespace HotelOrderSystem.Api.Entities;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
