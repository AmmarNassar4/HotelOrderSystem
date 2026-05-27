# Hotel Order & Task Management System

Visual Studio ready backend starter project for a hotel order and task management platform.

## What is included

- ASP.NET Core Web API targeting .NET 8
- SQL Server / EF Core data model
- JWT authentication
- Swagger / OpenAPI
- URL versioning with `/api/v1/...`
- Standard API envelope: `isSuccess`, `data`, `errorMessage`
- SignalR hubs for admin and staff realtime updates
- FCM notification outbox with a replaceable sender stub
- Staff presence heartbeat and online/offline cleanup
- Optimistic concurrency with SQL Server `rowversion`
- Soft delete for master data
- SQL Server JSON constraints for dynamic attributes
- Demo seed data

## Open in Visual Studio

1. Extract the ZIP.
2. Open `HotelOrderSystem.sln` in Visual Studio 2022.
3. Make sure the .NET 8 SDK is installed.
4. Restore NuGet packages.
5. Check `src/HotelOrderSystem.Api/appsettings.json`.
6. Run the `HotelOrderSystem.Api` profile.
7. Swagger opens automatically at `/swagger`.

## Default database

The default connection string uses SQL Server LocalDB:

```json
"Server=(localdb)\\MSSQLLocalDB;Database=HotelOrderSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

For a real SQL Server instance, replace `ConnectionStrings:DefaultConnection`.

The project is configured with:

```json
"Database": {
  "EnsureCreated": true,
  "SeedDemoData": true
}
```

This lets the API create the database schema on first run for development. For production, create migrations and set `EnsureCreated` to false.

## Demo logins

| Username | Password | Role |
| --- | --- | --- |
| admin | admin123 | Admin |
| housekeeping | staff123 | Staff |
| maintenance | staff123 | Staff |
| restaurant | staff123 | Staff |

## Important production TODOs

1. Change `Jwt:SigningKey` to a secure secret.
2. Replace `FirebasePushNotificationService` stub with Firebase Admin SDK or HTTP v1 implementation.
3. Add refresh tokens if long-lived mobile sessions are required.
4. Add real migrations instead of `EnsureCreated`.
5. Add audit details for every sensitive admin action.
6. Put secrets in environment variables or Key Vault, not appsettings.json.

## Useful endpoints

- `POST /api/v1/auth/login`
- `PUT /api/v1/auth/device-token`
- `GET /api/v1/orders/pending`
- `POST /api/v1/orders`
- `PUT /api/v1/orders/{id}/accept`
- `PUT /api/v1/orders/{id}/complete`
- `PUT /api/v1/presence/heartbeat`
- `GET /api/v1/rooms`
- `GET /api/v1/items`
- `POST /api/v1/guest/rooms/{directLinkPayload}/orders`

## Notes for mobile lite app

Use a native Android WebView shell for the UI, but keep FirebaseMessagingService native. The shell should pass login/device data to the WebView, receive silent FCM data messages, and call JavaScript to update the visible order list without polling.


## Web Frontend

A responsive English LTR web frontend has been added under:

`src/HotelOrderSystem.Api/wwwroot`

Run the API project from Visual Studio and open:

- `/` for the web app.
- `/swagger` for API documentation.
- `/#/guest/{DirectLinkPayload}` for guest ordering without login.

Included screens:

- Admin dashboard
- Orders board and filters
- Manual/proxy order creation
- Mobile-friendly staff task screen
- Rooms, items, teams, and users CRUD
- Online/offline staff presence
- Performance report
- Guest order page without authentication
- Dynamic item property builder with no raw JSON shown to users

The frontend is a static SPA, so there is no Node.js build step required.
