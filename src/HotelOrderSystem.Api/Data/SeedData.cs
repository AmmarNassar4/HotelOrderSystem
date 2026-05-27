using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Entities;
using HotelOrderSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, IPasswordService passwordService, CancellationToken cancellationToken = default)
    {
        if (await db.Teams.AnyAsync(cancellationToken))
        {
            return;
        }

        var housekeeping = new Team { Name = "Housekeeping", IsActive = true };
        var maintenance = new Team { Name = "Maintenance", IsActive = true };
        var restaurant = new Team { Name = "Restaurant", IsActive = true };
        db.Teams.AddRange(housekeeping, maintenance, restaurant);
        await db.SaveChangesAsync(cancellationToken);

        var admin = new User
        {
            FullName = "System Admin",
            UserName = "admin",
            Role = Roles.Admin,
            IsActive = true
        };
        admin.PasswordHash = passwordService.HashPassword(admin, "admin123");

        var hkStaff = new User
        {
            FullName = "Housekeeping Staff",
            UserName = "housekeeping",
            TeamId = housekeeping.TeamId,
            Role = Roles.Staff,
            IsActive = true
        };
        hkStaff.PasswordHash = passwordService.HashPassword(hkStaff, "staff123");

        var mtStaff = new User
        {
            FullName = "Maintenance Staff",
            UserName = "maintenance",
            TeamId = maintenance.TeamId,
            Role = Roles.Staff,
            IsActive = true
        };
        mtStaff.PasswordHash = passwordService.HashPassword(mtStaff, "staff123");

        var restaurantStaff = new User
        {
            FullName = "Restaurant Staff",
            UserName = "restaurant",
            TeamId = restaurant.TeamId,
            Role = Roles.Staff,
            IsActive = true
        };
        restaurantStaff.PasswordHash = passwordService.HashPassword(restaurantStaff, "staff123");

        db.Users.AddRange(admin, hkStaff, mtStaff, restaurantStaff);

        db.Rooms.AddRange(
            new Room { RoomNumber = "101", DirectLinkPayload = "room-101", IsActive = true },
            new Room { RoomNumber = "102", DirectLinkPayload = "room-102", IsActive = true },
            new Room { RoomNumber = "201", DirectLinkPayload = "room-201", IsActive = true });

        db.Items.AddRange(
            new Item { Name = "Extra Towels", Type = ItemTypes.Product, TargetTeamId = housekeeping.TeamId, BaseProperties = "{\"__schemaVersion\":1,\"fields\":[{\"key\":\"size\",\"label\":\"Size\",\"type\":\"select\",\"required\":true,\"options\":[\"Small\",\"Medium\",\"Large\"],\"defaultValue\":\"Large\"}]}", IsActive = true },
            new Item { Name = "Plumbing Repair", Type = ItemTypes.Service, TargetTeamId = maintenance.TeamId, BaseProperties = "{\"__schemaVersion\":1,\"fields\":[{\"key\":\"urgency\",\"label\":\"Urgency\",\"type\":\"select\",\"required\":true,\"options\":[\"Normal\",\"High\",\"Emergency\"],\"defaultValue\":\"Normal\"},{\"key\":\"issue_notes\",\"label\":\"Issue notes\",\"type\":\"notes\",\"required\":false}]}", IsActive = true },
            new Item { Name = "Burger", Type = ItemTypes.Product, TargetTeamId = restaurant.TeamId, BaseProperties = "{\"__schemaVersion\":1,\"fields\":[{\"key\":\"spicy\",\"label\":\"Spicy\",\"type\":\"boolean\",\"required\":false,\"defaultValue\":false},{\"key\":\"doneness\",\"label\":\"Doneness\",\"type\":\"select\",\"required\":false,\"options\":[\"Medium\",\"Well done\"]}]}", IsActive = true },
            new Item { Name = "General Assistance", Type = ItemTypes.Service, TargetTeamId = null, BaseProperties = "{\"__schemaVersion\":1,\"fields\":[{\"key\":\"notes\",\"label\":\"Request notes\",\"type\":\"notes\",\"required\":false}]}", IsActive = true });

        await db.SaveChangesAsync(cancellationToken);
    }
}
