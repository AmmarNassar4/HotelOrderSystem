# Hotel Order Staff Android App

A native Android app for hotel operational staff (housekeeping, maintenance,
room service, etc.). It integrates directly with the ASP.NET Core backend over
REST, SignalR, and Firebase Cloud Messaging — built to the spec in
`docs/native-staff-mobile-app-requirements.md`.

## Stack

- Kotlin + Jetpack Compose (Material 3)
- MVVM + repository pattern, Hilt for DI
- Retrofit + OkHttp + kotlinx-serialization (REST)
- microsoft-signalr (realtime), Firebase Cloud Messaging (push)
- EncryptedSharedPreferences (secure token storage)
- Coroutines + Flow; Navigation-Compose
- Min SDK 23, target/compile SDK 35 (core-library desugaring for `java.time`)

## Architecture

```
data/
  local/SessionManager            secure session (token, profile, deviceId, ready)
  remote/                         Retrofit service, DTOs, envelope + NetworkResult,
                                  auth + unauthorized interceptors
  remote/mapper/                  DTO -> domain mappers
  repository/                     Auth / Device / Order / Presence impls
  realtime/SignalRManager         staff hub client + reconnect
domain/                           models, repository interfaces, RealtimeService
presence/PresenceManager          lifecycle-aware heartbeat (ProcessLifecycleOwner)
push/                             FCM service, token registrar, payload parsing
monitor/NetworkMonitor            connectivity
ui/                               Compose screens + ViewModels
  auth, splash, main (bottom nav), orders (pending/my-tasks/details),
  status, profile, navigation, theme, common
di/                               Hilt modules
```

## Screens

Splash → Login (role-gated; admins see Access Denied) → bottom-nav home:

- **Pending** — team orders as cards, pull-to-refresh, Accept (optimistic-
  concurrency `rowVersion`, 409 conflict handling)
- **My Tasks** — accepted orders, Complete (confirm + optional note), Details
- **Status** — Ready/Not-Ready toggle (optimistic, reverts on failure), online
  status, last heartbeat, team, connection + push status
- **Profile** — name/username/role/team/app version, full Logout
- **Order Details** — items + dynamic attributes, timestamps, status-based
  actions (Accept / Complete / Cancel for supervisors+admins)

## Backend integration

- Auth: `POST /api/v1/auth/login`, `POST /api/v1/auth/logout`,
  `PUT /api/v1/auth/device-token`
- Orders: `GET /orders/pending`, `GET /orders/my-active`, `GET /orders/{id}`,
  `PUT /orders/{id}/accept|complete|cancel`
- Presence: `PUT /presence/heartbeat` (every 60s, foreground),
  `PUT /presence/availability`
- Realtime: SignalR `/hubs/staff?access_token={jwt}` — events `OrderCreated`,
  `OrderAccepted`, `OrderCompleted` trigger list refreshes
- All responses use the `{ isSuccess, data, errorMessage }` envelope; 401 forces
  logout, 409 shows a conflict + refresh, offline shows a banner

## Firebase setup

The real Firebase file is git-ignored. Copy yours to:

```text
android-staff-app/app/google-services.json
```

The Firebase Android package name must be `com.ibaapps.HotelOrderSystem`
(matches `applicationId`). A non-functional placeholder is committed only so the
project compiles; replace it for real push delivery.

## Configuration (build-time)

`app/build.gradle.kts` `buildConfigField`s:

| Field | Debug | Release |
| --- | --- | --- |
| `API_BASE_URL` | `https://10.0.2.2:5001` | `https://hos.ibaapps.work` |
| `SIGNALR_STAFF_HUB` | `/hubs/staff` | `/hubs/staff` |
| `PLATFORM` | `Android` | `Android` |

`10.0.2.2` is the emulator's alias for the host machine. Release is HTTPS-only
(network security config denies cleartext); the debug build permits cleartext to
`10.0.2.2`/`localhost` and trusts user CAs so a local ASP.NET dev certificate
can be used.

## Build & run

Requires JDK 21 and the Android SDK (platform 35).

```bash
./gradlew :app:assembleDebug          # debug APK
./gradlew :app:testDebugUnitTest      # unit tests
./gradlew :app:assembleRelease        # minified release APK
```

Or open `android-staff-app` in Android Studio and run the `app` configuration
against an emulator with the backend running (seed users `housekeeping`,
`maintenance`, `restaurant` / password `staff123`).

## Demo staff logins

| Username | Password | Role |
| --- | --- | --- |
| housekeeping | staff123 | Staff |
| maintenance | staff123 | Staff |
| restaurant | staff123 | Staff |
