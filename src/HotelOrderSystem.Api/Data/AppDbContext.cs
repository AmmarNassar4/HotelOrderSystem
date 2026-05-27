using HotelOrderSystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<UserPresence> UserPresences => Set<UserPresence>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");
            entity.HasKey(x => x.TeamId);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UserName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasOne(x => x.Team)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Rooms");
            entity.HasKey(x => x.RoomId);
            entity.Property(x => x.RoomNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DirectLinkPayload).HasMaxLength(250).IsRequired();
            entity.HasIndex(x => x.RoomNumber).IsUnique();
            entity.HasIndex(x => x.DirectLinkPayload).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.ToTable("ItemCategories");
            entity.HasKey(x => x.ItemCategoryId);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items", table =>
            {
                table.HasCheckConstraint("CK_Items_BaseProperties_IsJson", "ISJSON([BaseProperties]) = 1");
            });
            entity.HasKey(x => x.ItemId);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ItemCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.BaseProperties).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne(x => x.TargetTeam)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.TargetTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.OrderId);
            entity.Property(x => x.Source).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.AssignedTeamId, x.Status, x.CreatedAt });
            entity.HasOne(x => x.Room)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedTeam)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.AssignedTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcceptedByUser)
                .WithMany()
                .HasForeignKey(x => x.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails", table =>
            {
                table.HasCheckConstraint("CK_OrderDetails_DynamicAttributes_IsJson", "ISJSON([DynamicAttributes]) = 1");
                table.HasCheckConstraint("CK_OrderDetails_Quantity_Positive", "[Quantity] > 0");
            });
            entity.HasKey(x => x.OrderDetailId);
            entity.Property(x => x.DynamicAttributes).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne(x => x.Order)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item)
                .WithMany(x => x.OrderDetails)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.ToTable("UserDevices");
            entity.HasKey(x => x.UserDeviceId);
            entity.Property(x => x.DeviceId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FcmToken).HasMaxLength(1000);
            entity.Property(x => x.Platform).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AppVersion).HasMaxLength(50);
            entity.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPresence>(entity =>
        {
            entity.ToTable("UserPresences");
            entity.HasKey(x => x.UserPresenceId);
            entity.Property(x => x.LastConnectionId).HasMaxLength(200);
            entity.Property(x => x.LastKnownAppState).HasMaxLength(50);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationOutbox>(entity =>
        {
            entity.ToTable("NotificationOutbox", table =>
            {
                table.HasCheckConstraint("CK_NotificationOutbox_PayloadJson_IsJson", "ISJSON([PayloadJson]) = 1");
            });
            entity.HasKey(x => x.NotificationId);
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(1000);
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.AuditLogId);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Details).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.CreatedAt);
        });
    }

    public override int SaveChanges()
    {
        ApplySoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplySoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }
    }
}
