using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Entities;

namespace HotelOrderSystem.Api.Services;

public interface IJwtTokenService
{
    AuthResponse CreateToken(User user);
}
