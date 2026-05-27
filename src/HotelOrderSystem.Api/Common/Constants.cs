namespace HotelOrderSystem.Api.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Supervisor = "Supervisor";
    public const string Staff = "Staff";
}

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class OrderSources
{
    public const string GuestQr = "GuestQR";
    public const string Admin = "Admin";
    public const string StaffProxy = "StaffProxy";
}

public static class ItemTypes
{
    public const string Product = "Product";
    public const string Service = "Service";
}

public static class NotificationStatuses
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public static class NotificationTypes
{
    public const string OrderCreated = "ORDER_CREATED";
    public const string OrderAccepted = "ORDER_ACCEPTED";
    public const string OrderCompleted = "ORDER_COMPLETED";
    public const string OrderClaimed = "ORDER_CLAIMED";
    public const string SlaEscalated = "SLA_ESCALATED";
}
