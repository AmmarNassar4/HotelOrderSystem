using HotelOrderSystem.Api.Entities;

namespace HotelOrderSystem.Api.Services;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password);
}
