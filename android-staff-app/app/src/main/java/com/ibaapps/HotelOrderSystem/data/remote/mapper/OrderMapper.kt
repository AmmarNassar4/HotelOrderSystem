package com.ibaapps.HotelOrderSystem.data.remote.mapper

import com.ibaapps.HotelOrderSystem.data.remote.dto.OrderDetailDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.OrderDto
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.OrderLine
import com.ibaapps.HotelOrderSystem.domain.model.OrderStatus
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.contentOrNull

private val attrJson = Json { ignoreUnknownKeys = true }

fun OrderDto.toDomain(): Order = Order(
    orderId = orderId,
    roomId = roomId,
    roomNumber = roomNumber,
    assignedTeamId = assignedTeamId,
    assignedTeamName = assignedTeamName,
    source = source,
    status = OrderStatus.fromRaw(status),
    statusRaw = status,
    createdByUserName = createdByUserName,
    acceptedByUserId = acceptedByUserId,
    acceptedByUserName = acceptedByUserName,
    createdAt = createdAtUtc.toUtcInstantOrNull(),
    acceptedAt = acceptedAtUtc.toUtcInstantOrNull(),
    completedAt = completedAtUtc.toUtcInstantOrNull(),
    slaDueAt = slaDueAtUtc.toUtcInstantOrNull(),
    escalatedAt = escalatedAtUtc.toUtcInstantOrNull(),
    rowVersion = rowVersion,
    details = details.map { it.toDomain() }
)

fun OrderDetailDto.toDomain(): OrderLine = OrderLine(
    orderDetailId = orderDetailId,
    itemId = itemId,
    itemName = itemName,
    quantity = quantity,
    dynamicAttributes = parseDynamicAttributes(dynamicAttributes)
)

/**
 * Backend stores dynamic item attributes as a JSON object string. Flattens it
 * into a display map; primitive values become their text, others their JSON.
 */
fun parseDynamicAttributes(raw: String?): Map<String, String> {
    if (raw.isNullOrBlank()) return emptyMap()
    val element = runCatching { attrJson.parseToJsonElement(raw) }.getOrNull()
    val obj = element as? JsonObject ?: return emptyMap()
    return obj.entries.associate { (key, value) ->
        val text = when (value) {
            is JsonPrimitive -> value.contentOrNull ?: value.toString()
            else -> value.toString()
        }
        key to text
    }
}
