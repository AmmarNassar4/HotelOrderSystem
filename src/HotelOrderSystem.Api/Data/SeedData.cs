using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Entities;
using HotelOrderSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Data;

public static class SeedData
{
    private const string AdminUserName = "admin";

    public static async Task EnsureSeededAsync(AppDbContext db, IPasswordService passwordService, string? adminPassword = null, CancellationToken cancellationToken = default)
    {
        var admin = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserName == AdminUserName, cancellationToken);

        if (admin is null && string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Seed:AdminPassword must be configured before creating the initial admin account.");
        }

        if (admin is null)
        {
            admin = new User
            {
                FullName = "System Admin",
                UserName = AdminUserName,
                Role = Roles.Admin,
                TeamId = null,
                IsActive = true,
                IsDeleted = false
            };
            db.Users.Add(admin);
        }
        else
        {
            admin.Role = Roles.Admin;
            admin.TeamId = null;
            admin.IsActive = true;
            admin.IsDeleted = false;
        }

        if (!string.IsNullOrWhiteSpace(adminPassword))
        {
            admin.PasswordHash = passwordService.HashPassword(admin, adminPassword);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
