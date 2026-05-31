<div align="center">

# Hotel Order — Staff Android App

**A native Android client for hotel operational staff** — housekeeping, maintenance, room service, and other teams that receive and fulfil guest requests.

Built with Kotlin and Jetpack Compose, integrating directly with the ASP.NET Core backend over REST, SignalR, and Firebase Cloud Messaging.

[![Platform](https://img.shields.io/badge/platform-Android-3DDC84)](https://developer.android.com)
[![Language](https://img.shields.io/badge/language-Kotlin-7F52FF)](https://kotlinlang.org)
[![UI](https://img.shields.io/badge/UI-Jetpack%20Compose-4285F4)](https://developer.android.com/jetpack/compose)
[![Min SDK](https://img.shields.io/badge/minSdk-23-blue)](https://developer.android.com)
[![Target SDK](https://img.shields.io/badge/targetSdk-35-blue)](https://developer.android.com)

</div>

---

## Overview

This app gives staff a fast, mobile-first way to handle hotel requests on the floor: sign in, mark availability, receive real-time and push notifications for new team requests, and accept, view, complete, or cancel orders. It is a fully native client (not a WebView wrapper) implemented to the specification in [`docs/native-staff-mobile-app-requirements.md`](../docs/native-staff-mobile-app-requirements.md).

The app is intended for **Staff** and **Supervisor** roles only; admin accounts are shown an access-denied screen.

## Features

- **Secure authentication** — JWT login with the token stored in `EncryptedSharedPreferences`; role-gated access; full logout teardown.
- **Availability & presence** — Ready / Not-Ready toggle with optimistic UI, plus a lifecycle-aware 60-second heartbeat that distinguishes *Ready*, *Online*, and *Push-reachable* states.
- **Order lifecycle** — pending team orders, accept (with optimistic-concurrency conflict handling), my active tasks, complete with optional note, and supervisor/admin cancel.
- **Real-time updates** — SignalR staff hub keeps the pending and active lists current, with automatic reconnect and a "Reconnecting…" indicator.
- **Push notifications** — Firebase Cloud Messaging with deep links straight to the relevant order, deduplicated per order, working in both foreground and background.
- **Resilient networking** — offline banner, disabled actions while offline, cached "last updated" lists, and forced re-login on session expiry.

## Tech Stack

| Concern | Choice |
| --- | --- |
| Language / UI | Kotlin · Jetpack Compose (Material 3) |
| Architecture | MVVM · repository pattern · unidirectional state |
| Dependency injection | Hilt |
| Networking | Retrofit · OkHttp · kotlinx-serialization |
| Real-time | `com.microsoft.signalr` |
| Push | Firebase Cloud Messaging |
| Secure storage | AndroidX Security (`EncryptedSharedPreferences`) |
| Async | Coroutines · Flow |
| Navigation | Navigation-Compose |

Min SDK 23 · compile/target SDK 35 · core-library desugaring for `java.time`.

## Architecture

The codebase follows a layered MVVM structure with a clear domain boundary:

```
com.ibaapps.HotelOrderSystem
├── data
│   ├── local/SessionManager        Encrypted session (token, profile, deviceId, ready)
│   ├── remote                      Retrofit service, DTOs, ApiResponse envelope,
│   │                               NetworkResult mapping, auth/401 interceptors
│   ├── remote/mapper               DTO → domain mappers
│   ├── repository                  Auth · Device · Order · Presence implementations
│   └── realtime/SignalRManager     Staff-hub client with reconnect
├── domain                          Models, repository interfaces, RealtimeService
├── presence/PresenceManager        Lifecycle-aware heartbeat (ProcessLifecycleOwner)
├── push                            FCM service, token registrar, payload parsing
├── monitor/NetworkMonitor          Connectivity state
├── ui                              Compose screens + ViewModels
│   ├── auth · splash · main        (bottom navigation)
│   ├── orders                      pending · my-tasks · details
│   └── status · profile · theme    navigation · common
└── di                              Hilt modules
```

**Principles:** ViewModels expose immutable UI state via `StateFlow`; repositories return a typed `NetworkResult` that maps HTTP status to UI behavior; the realtime and connectivity layers sit behind interfaces so ViewModels stay transport-agnostic and unit-testable.

## Screens

`Splash` → `Login` (role-gated) → bottom-navigation home:

| Tab | Purpose |
| --- | --- |
| **Pending** | Team orders as cards · pull-to-refresh · Accept (handles 409 conflicts) |
| **My Tasks** | Accepted orders · Complete (confirm + optional note) · Details |
| **Status** | Ready toggle · online/connection state · last heartbeat · team · push status |
| **Profile** | Name · username · role · team · app version · Logout |

`Order Details` (deep-linkable): items and dynamic attributes, timestamps, and status-appropriate actions (Accept · Complete · Cancel).

## Backend Integration

All responses use the standard `{ isSuccess, data, errorMessage }` envelope.

| Area | Endpoints |
| --- | --- |
| Auth | `POST /api/v1/auth/login` · `POST /api/v1/auth/logout` · `PUT /api/v1/auth/device-token` |
| Orders | `GET /orders/pending` · `GET /orders/my-active` · `GET /orders/{id}` · `PUT /orders/{id}/accept｜complete｜cancel` |
| Presence | `PUT /presence/heartbeat` (60 s, foreground) · `PUT /presence/availability` |
| Real-time | SignalR `/hubs/staff?access_token={jwt}` → `OrderCreated` · `OrderAccepted` · `OrderCompleted` |

**Error handling:** `401` forces logout · `403` access denied · `409` conflict message + refresh · `5xx` general error · network failure shows an offline banner without logging the user out.

## Getting Started

### Prerequisites

- Android Studio (Ladybug or newer) with **JDK 21**
- Android SDK **platform 35**
- A running instance of the Hotel Order backend

### 1. Firebase configuration

The real `google-services.json` is intentionally git-ignored. Add yours at:

```
android-staff-app/app/google-services.json
```

The Firebase Android package name **must** be `com.ibaapps.HotelOrderSystem` (it matches `applicationId`). A non-functional placeholder is committed so the project compiles out of the box — replace it to enable real push delivery.

### 2. Build configuration

Per-build-type values are defined in `app/build.gradle.kts`:

| Field | Debug | Release |
| --- | --- | --- |
| `API_BASE_URL` | `https://10.0.2.2:5001` | `https://hos.ibaapps.work` |
| `SIGNALR_STAFF_HUB` | `/hubs/staff` | `/hubs/staff` |
| `PLATFORM` | `Android` | `Android` |

`10.0.2.2` is the Android emulator's alias for the host machine. **Release builds are HTTPS-only** (the network-security config denies cleartext); the debug build additionally permits cleartext to `10.0.2.2`/`localhost` and trusts user-installed CAs so a local ASP.NET dev certificate can be used.

### 3. Build & test

```bash
./gradlew :app:assembleDebug        # debug APK
./gradlew :app:testDebugUnitTest    # unit tests
./gradlew :app:assembleRelease      # minified, shrunk release APK
```

Or open `android-staff-app` in Android Studio and run the `app` configuration against an emulator with the backend running.

### Demo staff logins

When the backend is seeded with demo data:

| Username | Password | Role |
| --- | --- | --- |
| `housekeeping` | `staff123` | Staff |
| `maintenance` | `staff123` | Staff |
| `restaurant` | `staff123` | Staff |

## Testing

Unit tests cover the session store, network result mapping, DTO→domain mappers, FCM payload parsing, and the screen ViewModels (login/role gate, pending accept + conflict, complete, order details, status toggle, profile logout):

```bash
./gradlew :app:testDebugUnitTest
```

## Security

- JWT stored in `EncryptedSharedPreferences`; passwords are never persisted.
- Tokens and FCM tokens are never logged (HTTP logging is `BASIC` in debug with the `Authorization` header redacted, and disabled entirely in release).
- HTTPS-only in release; logout deactivates the device token on the backend.
- No admin functionality is exposed; the API base URL is fixed at build time.

## Project Status

All features in the requirements specification are implemented. Functional end-to-end verification (push delivery, SignalR, live device flows) requires a real `google-services.json` and a running backend. Continuous active-duty mode via an Android Foreground Service (the spec's optional "Option B") is intentionally out of scope for v1.
