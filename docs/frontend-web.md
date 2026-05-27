# Web Frontend

The web frontend is a static responsive SPA hosted inside `HotelOrderSystem.Api/wwwroot`.

## Routes

- `#/login`
- `#/admin/dashboard`
- `#/admin/orders`
- `#/admin/create-order`
- `#/admin/performance`
- `#/admin/presence`
- `#/admin/rooms`
- `#/admin/items`
- `#/admin/teams`
- `#/admin/users`
- `#/staff/tasks`
- `#/staff/create-order`
- `#/guest/{DirectLinkPayload}`

## Dynamic item fields

Item properties are still saved in `Items.BaseProperties` as JSON, but users never edit raw JSON in the web interface.

Admins define fields through the Item Field Builder:

- Field label
- API key
- Input type
- Required flag
- Default value
- Choices for single or multiple choice fields

When an admin, staff member, or guest creates an order, the web app renders those definitions as normal inputs, selects, checkboxes, and text areas. The submitted order line is stored in `OrderDetails.DynamicAttributes` as JSON for API consistency.

## Guest QR

From Rooms, copy the guest link and convert it into a QR code for the room.

## Mobile / WebView

The same web frontend works inside Android WebView. The native Android shell remains responsible for real FCM handling and can call `PUT /api/v1/auth/device-token` after login.

## Realtime

The frontend uses SignalR when the client script is available and falls back to lightweight polling.
