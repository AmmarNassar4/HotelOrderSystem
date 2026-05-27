# Hotel Order Staff Android App

Very light native Android app for staff. It uses a native WebView shell for the web staff UI and Firebase Cloud Messaging for order notifications.

## Stack

- Kotlin
- Android WebView
- Firebase Cloud Messaging
- Kotlin DSL Gradle files

## Firebase setup

The real Firebase file is intentionally ignored by git.

Copy your Firebase file to:

```text
android-staff-app/app/google-services.json
```

The Firebase Android package name must match:

```text
com.ibaapps.HotelOrderSystem
```

This app's `applicationId` is already configured with the same value.

## Configure URLs

Edit `android-staff-app/app/build.gradle.kts`:

```kotlin
buildConfigField("String", "WEB_APP_URL", '"https://your-domain.com/#/staff"')
buildConfigField("String", "API_BASE_URL", '"https://your-domain.com"')
```

Debug currently points to:

```text
https://10.0.2.2:5001/#/staff
https://10.0.2.2:5001
```

`10.0.2.2` is the Android Emulator address for your host machine.

## Ready / Not Ready behavior

The native app has a top Ready switch.

- Ready: calls `PUT /api/v1/presence/availability` with `isReady=true`.
- Not Ready: calls the same endpoint with `isReady=false`.
- Staff pending orders are hidden by the backend while not ready.
- FCM team notifications are sent only to users marked ready.
- Ready and not-ready time is stored in `StaffAvailabilityLogs` for future performance reports.

## Web bridge

The app injects `window.HotelNativeReady` into the WebView:

```js
window.HotelNativeReady.setReady(true)
window.HotelNativeReady.setReady(false)
window.HotelNativeReady.getDeviceId()
window.HotelNativeReady.isReady()
```

The app also reads the web login session from localStorage key:

```text
hotel.ops.session
```

and registers the FCM token with:

```text
PUT /api/v1/auth/device-token
```

## Build

Open `android-staff-app` in Android Studio, sync Gradle, then run the `app` configuration.
