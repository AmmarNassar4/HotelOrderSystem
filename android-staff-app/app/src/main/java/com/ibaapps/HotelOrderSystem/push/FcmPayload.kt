package com.ibaapps.HotelOrderSystem.push

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.intOrNull

private val payloadJson = Json { ignoreUnknownKeys = true }

/**
 * Extracts an order id from an FCM data message. The backend may put it
 * directly as `orderId`, or nested inside a JSON `payload` string
 * (see docs/android-webview-integration.md).
 */
fun parseOrderIdFromData(data: Map<String, String>): Int? {
    data["orderId"]?.trim()?.toIntOrNull()?.let { return it }
    val payload = data["payload"] ?: return null
    return runCatching {
        payloadJson.parseToJsonElement(payload).jsonObject["orderId"]?.jsonPrimitive?.intOrNull
    }.getOrNull()
}

/** Maps a backend notification type to a user-facing title. */
fun notificationTitleFor(type: String?): String = when (type) {
    "OrderCreated" -> "New order"
    "OrderClaimed", "OrderAccepted" -> "Order claimed"
    "OrderCompleted" -> "Order completed"
    "OrderCancelled" -> "Order cancelled"
    else -> "Hotel order update"
}
