# API Flow Summary

## Authentication

- `POST /api/v1/auth/login`
- `PUT /api/v1/auth/device-token`
- `POST /api/v1/auth/logout`

Demo users when seed data is enabled:

| Username | Password | Role |
| --- | --- | --- |
| admin | admin123 | Admin |
| housekeeping | staff123 | Staff |
| maintenance | staff123 | Staff |
| restaurant | staff123 | Staff |

## Orders

- `POST /api/v1/orders`: creates proxy/admin/staff order.
- `POST /api/v1/guest/rooms/{directLinkPayload}/orders`: creates direct guest QR order.
- `GET /api/v1/orders/pending`: returns pending orders filtered by user's team.
- `PUT /api/v1/orders/{id}/accept`: accepts order using optimistic concurrency.
- `PUT /api/v1/orders/{id}/complete`: completes an accepted order.

## Realtime

SignalR hubs:

- `/hubs/admin`
- `/hubs/staff`

Events:

- `OrderCreated`
- `OrderAccepted`
- `OrderCompleted`
- `StaffPresenceChanged`
- `DashboardChanged`

## Presence

- Mobile sends heartbeat every 60 seconds.
- Server marks user offline when no heartbeat is received within the configured timeout.
- Default timeout is 120 seconds.

## FCM

The project includes an outbox and a stub `FirebasePushNotificationService`. Replace the stub sender with Firebase Admin SDK or HTTP v1 sender before production.
