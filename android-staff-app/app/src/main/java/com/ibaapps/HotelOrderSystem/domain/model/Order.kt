package com.ibaapps.HotelOrderSystem.domain.model

import java.time.Instant

enum class OrderStatus {
    Pending,
    Accepted,
    InProgress,
    Completed,
    Cancelled,
    Unknown;

    companion object {
        fun fromRaw(raw: String?): OrderStatus = when (raw?.trim()?.lowercase()) {
            "pending" -> Pending
            "accepted" -> Accepted
            "inprogress", "in_progress", "in progress" -> InProgress
            "completed" -> Completed
            "cancelled", "canceled" -> Cancelled
            else -> Unknown
        }
    }
}

data class Order(
    val orderId: Int,
    val roomId: Int,
    val roomNumber: String,
    val assignedTeamId: Int?,
    val assignedTeamName: String?,
    val source: String,
    val status: OrderStatus,
    val statusRaw: String,
    val createdByUserName: String?,
    val acceptedByUserId: Int?,
    val acceptedByUserName: String?,
    val createdAt: Instant?,
    val acceptedAt: Instant?,
    val completedAt: Instant?,
    val slaDueAt: Instant?,
    val escalatedAt: Instant?,
    val rowVersion: String,
    val details: List<OrderLine>
) {
    /** Primary item/service name for compact cards (first line, or a count summary). */
    val primaryItemName: String
        get() = when {
            details.isEmpty() -> "Request"
            details.size == 1 -> details.first().itemName
            else -> "${details.first().itemName} +${details.size - 1}"
        }

    val isEscalated: Boolean get() = escalatedAt != null
}

data class OrderLine(
    val orderDetailId: Int,
    val itemId: Int,
    val itemName: String,
    val quantity: Int,
    val dynamicAttributes: Map<String, String>
)
