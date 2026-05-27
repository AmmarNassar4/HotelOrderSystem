-- Hotel Order & Task Management System - SQL Server schema
-- You can let EF Core create the schema by setting Database:EnsureCreated=true.
-- This script is included for DBAs or manual SQL Server setup.

IF DB_ID(N'HotelOrderSystemDb') IS NULL
BEGIN
    CREATE DATABASE HotelOrderSystemDb;
END
GO

USE HotelOrderSystemDb;
GO

CREATE TABLE Teams (
    TeamId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL CONSTRAINT UQ_Teams_Name UNIQUE,
    IsActive BIT NOT NULL CONSTRAINT DF_Teams_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Teams_IsDeleted DEFAULT 0
);
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL,
    UserName NVARCHAR(80) NOT NULL CONSTRAINT UQ_Users_UserName UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    TeamId INT NULL,
    Role NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
    CONSTRAINT FK_Users_Teams FOREIGN KEY (TeamId) REFERENCES Teams(TeamId)
);
GO

CREATE TABLE Rooms (
    RoomId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
    RoomNumber NVARCHAR(50) NOT NULL CONSTRAINT UQ_Rooms_RoomNumber UNIQUE,
    DirectLinkPayload NVARCHAR(250) NOT NULL CONSTRAINT UQ_Rooms_DirectLinkPayload UNIQUE,
    IsActive BIT NOT NULL CONSTRAINT DF_Rooms_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Rooms_IsDeleted DEFAULT 0
);
GO

CREATE TABLE Items (
    ItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Items PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    TargetTeamId INT NULL,
    BaseProperties NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Items_BaseProperties DEFAULT N'{}',
    IsActive BIT NOT NULL CONSTRAINT DF_Items_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Items_IsDeleted DEFAULT 0,
    CONSTRAINT FK_Items_Teams FOREIGN KEY (TargetTeamId) REFERENCES Teams(TeamId),
    CONSTRAINT CK_Items_BaseProperties_IsJson CHECK (ISJSON(BaseProperties) = 1)
);
GO

CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    RoomId INT NOT NULL,
    CreatedByUserId INT NULL,
    AssignedTeamId INT NULL,
    Source NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    AcceptedByUserId INT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
    AcceptedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    CancelledAt DATETIME2 NULL,
    SlaDueAt DATETIME2 NULL,
    EscalatedAt DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Orders_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Orders_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_AcceptedByUser FOREIGN KEY (AcceptedByUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Teams FOREIGN KEY (AssignedTeamId) REFERENCES Teams(TeamId)
);
GO

CREATE INDEX IX_Orders_AssignedTeamId_Status_CreatedAt ON Orders(AssignedTeamId, Status, CreatedAt);
GO

CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId INT NOT NULL,
    ItemId INT NOT NULL,
    Quantity INT NOT NULL,
    DynamicAttributes NVARCHAR(MAX) NOT NULL CONSTRAINT DF_OrderDetails_DynamicAttributes DEFAULT N'{}',
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Items FOREIGN KEY (ItemId) REFERENCES Items(ItemId),
    CONSTRAINT CK_OrderDetails_Quantity_Positive CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetails_DynamicAttributes_IsJson CHECK (ISJSON(DynamicAttributes) = 1)
);
GO

CREATE TABLE UserDevices (
    UserDeviceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserDevices PRIMARY KEY,
    UserId INT NOT NULL,
    DeviceId NVARCHAR(200) NOT NULL,
    FcmToken NVARCHAR(1000) NULL,
    Platform NVARCHAR(50) NOT NULL,
    AppVersion NVARCHAR(50) NOT NULL,
    LastSeenAt DATETIME2 NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UserDevices_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserDevices_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserDevices_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserDevices_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT UQ_UserDevices_UserId_DeviceId UNIQUE (UserId, DeviceId)
);
GO

CREATE TABLE UserPresences (
    UserPresenceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPresences PRIMARY KEY,
    UserId INT NOT NULL CONSTRAINT UQ_UserPresences_UserId UNIQUE,
    IsOnline BIT NOT NULL CONSTRAINT DF_UserPresences_IsOnline DEFAULT 0,
    LastHeartbeatAt DATETIME2 NULL,
    LastConnectionId NVARCHAR(200) NULL,
    LastKnownAppState NVARCHAR(50) NULL,
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserPresences_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserPresences_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

CREATE TABLE NotificationOutbox (
    NotificationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationOutbox PRIMARY KEY,
    Type NVARCHAR(100) NOT NULL,
    TargetUserId INT NULL,
    TargetTeamId INT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_NotificationOutbox_PayloadJson DEFAULT N'{}',
    Status NVARCHAR(50) NOT NULL,
    RetryCount INT NOT NULL CONSTRAINT DF_NotificationOutbox_RetryCount DEFAULT 0,
    LastError NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_NotificationOutbox_CreatedAt DEFAULT SYSUTCDATETIME(),
    SentAt DATETIME2 NULL,
    CONSTRAINT CK_NotificationOutbox_PayloadJson_IsJson CHECK (ISJSON(PayloadJson) = 1)
);
GO

CREATE INDEX IX_NotificationOutbox_Status_CreatedAt ON NotificationOutbox(Status, CreatedAt);
GO

CREATE TABLE AuditLogs (
    AuditLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId NVARCHAR(100) NOT NULL,
    Details NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO
