# Android Lite WebView Integration Notes

The backend is API-first and supports a very light Android shell:

1. Open your responsive staff web UI inside a WebView.
2. Use native FirebaseMessagingService to receive FCM data messages.
3. Register the device token with `PUT /api/v1/auth/device-token` after login.
4. Send heartbeat every 60 seconds while the app is foregrounded:

```http
PUT /api/v1/presence/heartbeat
Authorization: Bearer {jwt}
Content-Type: application/json

{
  "deviceId": "android-device-id",
  "appState": "foreground",
  "currentScreen": "orders"
}
```

5. When receiving a data message such as `ORDER_CLAIMED`, call the WebView JavaScript bridge to remove that order from the local list.
6. When the app opens, call `GET /api/v1/orders/pending` and `GET /api/v1/orders/my-active` to resync.

Recommended FCM data message keys:

```json
{
  "type": "ORDER_CLAIMED",
  "payload": "{\"orderId\":123,\"acceptedByUserId\":88,\"assignedTeamId\":3}"
}
```
