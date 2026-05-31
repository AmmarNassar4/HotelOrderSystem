# Native Staff Mobile App Requirements

## Hotel Order System

## 1. Project Overview

We need to build a native mobile application for hotel staff members. The app should provide the same core functionality as the existing Staff web page, but with a dedicated mobile-first user experience.

The app is not for hotel guests and not for admins. It is specifically for operational staff members such as housekeeping, maintenance, food and beverage, room service, or any other team that receives and handles hotel requests.

The goal is to allow staff members to:

- Log in securely.
- Mark themselves as Ready or Not Ready.
- Receive real-time requests assigned to their team.
- Receive push notifications when new requests arrive.
- Accept a request.
- View request details.
- Complete a request.
- Cancel or reject a request if allowed by the backend.
- Keep their online/ready status updated through heartbeat.
- Work efficiently from a phone during hotel operations.

The existing backend is an ASP.NET Core API. The native app should integrate directly with this API using HTTPS, JWT authentication, SignalR, and Firebase Cloud Messaging.

---

## 2. Target Platforms

The first version should support:

- Android native app

Optional later support:

- iOS native app

Recommended Android stack:

- Kotlin
- Jetpack Compose
- Retrofit or Ktor Client for REST API
- SignalR client for real-time updates
- Firebase Cloud Messaging for push notifications
- DataStore or EncryptedSharedPreferences for secure local storage

If the developer prefers cross-platform native:

- Flutter or React Native can be considered

However, the required output should behave like a real mobile app, not just a WebView wrapper.

---

## 3. User Type

The app is for authenticated staff users only.

Expected backend roles:

- Staff
- Supervisor, if supervisors also need staff-style task handling

Admin should not be the primary target of this app.

After login, the app should check the returned user role. If the user is not allowed to use the Staff app, show an access denied screen.

---

## 4. Authentication

### 4.1 Login Screen

The app should have a clean login screen with:

- Username field
- Password field
- Sign in button
- Loading state
- Error message area
- Server/API base URL configuration, either hidden in settings or configured at build time

The login API is:

```http
POST /api/v1/auth/login
```

Request body:

```json
{
  "userName": "hk",
  "password": "password"
}
```

The response contains a JWT token and user information. Store the token securely on the device.

The app should use:

```http
Authorization: Bearer {jwt_token}
```

for all authenticated API calls.

### 4.2 Logout

Logout should:

1. Call the backend logout endpoint.
2. Clear the stored JWT token.
3. Clear local user/session data.
4. Navigate back to the login screen.
5. Disconnect SignalR.
6. Stop heartbeat.

Logout API:

```http
POST /api/v1/auth/logout
```

Request body:

```json
{
  "deviceId": "unique-device-id"
}
```

---

## 5. Device Registration and Push Notifications

The native app must use Firebase Cloud Messaging.

After successful login:

1. Get the FCM token from Firebase.
2. Generate or read a persistent unique device ID.
3. Register the device token with the backend.

Endpoint:

```http
PUT /api/v1/auth/device-token
```

Request body:

```json
{
  "deviceId": "android-device-uuid",
  "platform": "Android",
  "appVersion": "1.0.0",
  "fcmToken": "firebase-token-here"
}
```

The app must register the FCM token:

- After login
- Whenever Firebase refreshes the token
- After app reinstall
- When the user logs in from a new device

Push notifications should open the app and navigate to the related order details screen if the payload contains an order ID.

Expected notification behavior:

- If the app is in foreground: show an in-app banner or dialog.
- If the app is in background: show a system notification.
- If the user taps the notification: open the order details.

---

## 6. Presence and Availability

The app must support staff availability.

There are two concepts:

1. Online status: the app is active and the backend recently received a heartbeat.
2. Ready status: the staff member is available to receive tasks.

### 6.1 Heartbeat

The app should send heartbeat periodically while the user is logged in.

Endpoint:

```http
PUT /api/v1/presence/heartbeat
```

Suggested interval:

```text
Every 60 seconds
```

Request body example:

```json
{
  "deviceId": "android-device-uuid",
  "platform": "Android"
}
```

The heartbeat should run:

- When the app is foregrounded
- While the user is logged in
- When the staff app screen is active

The heartbeat should stop:

- After logout
- When the token expires
- When the user is no longer authenticated

### 6.2 Ready / Not Ready Toggle

The main Staff screen must have a clear Ready toggle.

Endpoint:

```http
PUT /api/v1/presence/availability
```

Request body:

```json
{
  "isReady": true
}
```

or:

```json
{
  "isReady": false
}
```

UI requirements:

- When staff is Ready, show a clear green status.
- When staff is Not Ready, show a neutral or red/orange status.
- The current state should be visible at the top of the app.
- If the API call fails, revert the toggle and show an error.

### 6.3 Background Heartbeat Limitations and Native Android Requirements

The mobile app should not copy the browser/PWA heartbeat behavior directly.

In a web browser or PWA, it is not reliable to keep the app active in the background and send heartbeat requests continuously. Mobile operating systems and browsers may pause JavaScript timers, suspend background pages, throttle network activity, or stop the page completely when the screen is locked or the user switches to another app.

For this reason, the production Android app should treat heartbeat differently from the web app.

#### Web/PWA limitation

The web/PWA version can send heartbeat only while the app is open and active in the foreground.

The backend may mark a staff member as offline if no heartbeat is received within the configured timeout, for example 90 to 120 seconds.

This is expected behavior and should not be treated as a bug in the web version.

#### Native Android requirement

The Android native app should provide a more reliable presence model than the browser version.

The developer should implement one of the following approaches depending on the final business requirement.

#### Option A: Foreground-only heartbeat

This is the recommended default for version 1.

The app sends heartbeat when:

- The staff user is logged in.
- The app is in the foreground.
- The staff dashboard, pending tasks screen, or active tasks screen is open.
- The app resumes from background.

The app stops heartbeat when:

- The user logs out.
- The app goes to background.
- The token expires.
- The app is closed.

When the app returns to foreground, it should immediately:

1. Validate the stored session.
2. Send heartbeat.
3. Refresh pending orders.
4. Refresh active tasks.
5. Reconnect SignalR if needed.
6. Re-register or verify the FCM token if needed.

This approach is battery-friendly and acceptable if push notifications are used to alert staff while the app is not open.

#### Option B: Foreground Service heartbeat

Use this only if the hotel operation requires staff to remain actively online even when the app is not visible.

On Android, a true continuous background heartbeat should be implemented using a Foreground Service.

This service must show a persistent notification such as:

```text
Hotel Staff App is active
Ready to receive hotel requests
```

The Foreground Service can send heartbeat at a controlled interval, for example every 60 seconds, while the staff member is marked as Ready.

The service should stop when:

- The staff user logs out.
- The staff user switches to Not Ready.
- The user explicitly stops active duty mode.
- The app detects an invalid or expired token.

Important notes:

- Android may restrict background services depending on OS version, battery settings, and manufacturer-specific power management.
- A Foreground Service is more reliable than normal background tasks, but it must be visible to the user through a persistent notification.
- The app should not try to run silent continuous background work without user visibility.
- The developer must follow Android background execution policies.

#### Recommended business logic

The system should separate these states:

```text
Ready = The staff member says they are available for work.
Online = The system recently received heartbeat from the app.
Reachable = The user has a valid active FCM token and can receive push notifications.
```

A staff member can be:

```text
Ready = true
Online = false
Reachable = true
```

This means the staff member is not actively using the app right now, but they may still receive push notifications.

The backend and mobile app should not assume that Ready always means Online.

#### Suggested task routing behavior

For real-time task assignment, the system should prefer staff who are:

```text
Ready = true
Online = true
```

For push notification alerts, the system may notify staff who are:

```text
Ready = true
Reachable = true
```

If the business wants requests to be shown only to currently active staff, then the backend should require both Ready and Online.

If the business wants staff to receive notifications even when the app is closed, then the backend should use FCM tokens for Ready staff even if they are currently Offline.

#### Android implementation guidance

The Android developer should implement:

- App lifecycle detection.
- Immediate heartbeat on app foreground.
- Periodic heartbeat while foregrounded.
- SignalR reconnect on app resume.
- FCM token registration after login and token refresh.
- Clear handling of Ready, Online, and Push Reachable states.
- Optional Foreground Service only if continuous active duty mode is required.

The app should avoid aggressive background polling unless explicitly required.

#### Expected behavior

When the app is open:

- Heartbeat is sent periodically.
- SignalR remains connected.
- Staff appears Online.
- Staff can accept and complete tasks.

When the app is closed or backgrounded without Foreground Service:

- Heartbeat may stop.
- SignalR may disconnect.
- Staff may become Offline after the backend timeout.
- Staff can still receive FCM push notifications if the token is valid.

When the app is running in Foreground Service mode:

- A persistent Android notification is shown.
- Heartbeat may continue while the staff member is Ready.
- The user can stop active duty mode.
- The app must respect battery and Android background restrictions.

#### Acceptance criteria for Android heartbeat behavior

The Android app is acceptable when:

- It sends heartbeat immediately after login.
- It sends heartbeat when returning from background.
- It stops heartbeat after logout.
- It does not falsely show the staff as Online when heartbeat has stopped.
- It clearly displays Ready and Online as separate states.
- It receives push notifications even when not currently Online, if the user has a valid FCM token.
- If Foreground Service mode is implemented, it shows a persistent notification and stops correctly when the user is Not Ready or logged out.

---

## 7. Order and Task Management

The native app should focus on orders/tasks assigned to the staff member's team.

### 7.1 Pending Orders

Endpoint:

```http
GET /api/v1/orders/pending
```

This should return orders that are pending and visible to the staff member based on their team and role.

The Pending screen should show cards, not tables.

Each order card should show:

- Order ID
- Room number
- Request type or item/service name
- Quantity
- Order creation time
- SLA or urgency indicator, if available
- Current status
- Short description or item details
- Accept button

### 7.2 My Active Orders

Endpoint:

```http
GET /api/v1/orders/my-active
```

This screen should show orders already accepted or assigned to the logged-in staff member.

Each active order card should show:

- Order ID
- Room number
- Item/service name
- Status
- Started/accepted time, if available
- Complete button
- Details button

### 7.3 Order Details

Endpoint:

```http
GET /api/v1/orders/{id}
```

The details screen should show:

- Room number
- Order status
- Requested items/services
- Quantities
- Dynamic item fields, if present
- Notes, if present
- Created time
- Accepted by, if available
- Completed time, if available
- Action buttons depending on status

Actions:

- Accept
- Complete
- Cancel, if allowed
- Back

### 7.4 Accept Order

Endpoint:

```http
PUT /api/v1/orders/{id}/accept
```

Request body example:

```json
{
  "note": "Accepted from mobile app"
}
```

Behavior:

- Disable the button while the request is loading.
- If successful, move the order from Pending to My Active.
- If conflict occurs, show a message such as: "This order was already accepted by another staff member."

### 7.5 Complete Order

Endpoint:

```http
PUT /api/v1/orders/{id}/complete
```

Request body example:

```json
{
  "note": "Completed from mobile app"
}
```

Behavior:

- Ask for confirmation before completing.
- Optional note field can be shown.
- After success, remove it from active tasks or mark it as completed.

### 7.6 Cancel Order

Endpoint:

```http
PUT /api/v1/orders/{id}/cancel
```

Request body example:

```json
{
  "reason": "Unable to complete"
}
```

This action should only be shown if the backend allows the current user to cancel the order.

---

## 8. Real-Time Updates Using SignalR

The app should connect to the backend SignalR hub after login.

Possible hub:

```text
/hubs/staff
```

The connection should use the JWT token.

The web app currently sends the token using the `access_token` query parameter for SignalR connections. The native app should follow the same backend requirement unless the backend is changed to support Authorization headers for WebSocket negotiation.

Expected SignalR URL:

```text
https://your-domain.com/hubs/staff?access_token={jwt_token}
```

The app should listen for real-time events related to:

- New order assigned to the staff team
- Order updated
- Order accepted by someone else
- Order completed
- Order cancelled
- Presence updates, if needed

If the exact event names are not documented, the backend developer should expose or document the current SignalR event names used by the web app.

Real-time behavior:

- When a new order arrives, update Pending Orders immediately.
- When an order is accepted by another staff member, remove it from Pending Orders.
- When an active order changes, update its card/details.
- If SignalR disconnects, show a subtle "Reconnecting..." state and retry automatically.
- The app should still refresh from REST API when it resumes from background.

---

## 9. App Navigation

Recommended mobile navigation:

### Bottom Navigation Tabs

1. Pending
2. My Tasks
3. Status
4. Profile

### Pending Tab

Shows pending orders for the user's team.

Main actions:

- Pull to refresh
- Accept order
- Open details

### My Tasks Tab

Shows orders accepted by the current user.

Main actions:

- View details
- Complete order

### Status Tab

Shows:

- Ready / Not Ready toggle
- Online status
- Last heartbeat time
- Current team
- Connection status
- Push notification status

### Profile Tab

Shows:

- Staff full name
- Username
- Role
- Team
- App version
- Logout button

---

## 10. UI/UX Requirements

The app must be optimized for real hotel operations.

Design priorities:

- Large touch targets
- Clear task cards
- Fast access to Accept and Complete buttons
- Minimal typing
- Clear room number visibility
- Clear status colors
- Good readability in bright environments
- Works well on small Android phones

Suggested status colors:

- Pending: blue or neutral
- Accepted / In Progress: amber
- Completed: green
- Cancelled: red or gray
- Ready: green
- Not Ready: gray or orange
- Offline / disconnected: red

Order card design:

- Room number should be prominent.
- Item/service name should be prominent.
- Secondary data should be smaller.
- Action buttons should be at the bottom of the card.
- Avoid table layouts.

---

## 11. Offline and Error Handling

The app does not need full offline order processing in version 1, but it must handle weak network conditions gracefully.

Required behavior:

- Show "No internet connection" when offline.
- Disable actions when offline.
- Retry SignalR connection automatically.
- Retry heartbeat on next interval.
- Do not log the user out immediately on network errors.
- If the token is expired or invalid, navigate to login.
- Show clear API error messages.

Recommended behavior:

- Cache the latest Pending and My Tasks lists locally for display only.
- Mark cached data as "Last updated at ...".
- Refresh when the app returns online.

---

## 12. Security Requirements

- Store JWT token securely.
- Do not store passwords.
- Do not log JWT tokens.
- Do not log FCM tokens in production logs.
- Use HTTPS only.
- Validate server SSL certificate normally.
- Logout must clear local token and unregister/deactivate device token through backend.
- App should not expose admin functionality.
- App should not allow changing API base URL in production builds unless protected.

---

## 13. Background Behavior

When the app is backgrounded:

- Stop unnecessary polling unless Foreground Service active-duty mode is explicitly enabled.
- Keep push notifications enabled.
- Do not keep aggressive background heartbeat unless specifically required.
- On resume, immediately:
  - Check token
  - Send heartbeat
  - Refresh pending orders
  - Refresh active orders
  - Reconnect SignalR if needed

---

## 14. Push Notification Flow

Expected flow:

1. Guest or admin creates an order.
2. Backend routes order to the correct team.
3. Backend sends SignalR update to connected staff.
4. Backend sends FCM push to registered devices for ready staff.
5. Staff receives notification.
6. Staff taps notification.
7. App opens order details.
8. Staff accepts or completes the request.

The native app should support deep linking from notifications to:

```text
OrderDetails(orderId)
```

If the notification payload does not contain order ID, open Pending Orders tab.

---

## 15. API Response Handling

The backend commonly wraps responses in an API response object.

The app should expect a wrapper similar to:

```json
{
  "isSuccess": true,
  "data": {},
  "errorMessage": null
}
```

Implementation rule:

- If `isSuccess` is false, show `errorMessage`.
- If HTTP status is 401, force logout.
- If HTTP status is 403, show access denied.
- If HTTP status is 409, show conflict message and refresh data.
- If HTTP status is 500, show general server error.

---

## 16. Minimum Screens

Version 1 should include:

1. Splash screen
2. Login screen
3. Pending orders screen
4. My active tasks screen
5. Order details screen
6. Availability/status screen
7. Profile/logout screen
8. Notification permission screen or prompt
9. Connection/error state UI

---

## 17. Recommended Technical Structure

Suggested Android architecture:

- MVVM
- Repository pattern
- Use cases for order actions
- Retrofit API service
- SignalR service
- FCM service
- DataStore for session storage
- ViewModels per screen
- Kotlin coroutines and Flow
- Compose UI

Suggested modules/classes:

```text
AuthRepository
OrderRepository
PresenceRepository
DeviceRepository
SignalRManager
FcmTokenManager
SessionManager
NetworkMonitor
PendingOrdersViewModel
MyTasksViewModel
OrderDetailsViewModel
StatusViewModel
```

---

## 18. Configuration

The app should support these build-time configuration values:

```text
API_BASE_URL=https://your-domain.com
SIGNALR_STAFF_HUB=/hubs/staff
PLATFORM=Android
APP_VERSION=1.0.0
```

For development:

```text
API_BASE_URL=https://localhost:7188
```

For production:

```text
API_BASE_URL=https://hos.ibaapps.work
```

The exact production URL should be confirmed before release.

---

## 19. Acceptance Criteria

The app is considered ready when:

- Staff can log in successfully.
- Staff can register FCM token.
- Staff can toggle Ready/Not Ready.
- Heartbeat is sent successfully while the app is foregrounded, or through Foreground Service mode if that option is implemented.
- Pending team orders appear.
- Staff can accept an order.
- Accepted orders appear under My Tasks.
- Staff can complete an order.
- Staff receives push notifications for new requests.
- Staff receives real-time updates using SignalR.
- App handles expired token properly.
- App works on Android devices over production HTTPS.
- No duplicate notifications appear on the same device.
- The app remains usable with weak network conditions.
- The UI is clearly mobile-first.
- Ready, Online, and Push Reachable states are displayed and handled separately.

---

## 20. Notes for Backend Developer

The backend should confirm or document:

- Exact request/response DTOs for all endpoints.
- Exact SignalR event names and payloads.
- Whether Staff users can cancel orders.
- Whether order status includes "In Progress" or only accepted/completed/cancelled.
- Whether FCM payload contains `orderId`.
- Whether heartbeat should continue in background.
- Whether the native app should show only Ready staff tasks or all team tasks.
- Whether supervisors should use the same mobile app.

If any endpoint is missing or not ideal for mobile usage, the backend should add mobile-friendly endpoints rather than forcing the native app to mimic web-specific behavior.
