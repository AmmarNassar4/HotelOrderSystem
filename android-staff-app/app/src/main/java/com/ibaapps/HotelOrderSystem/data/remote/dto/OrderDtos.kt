package com.ibaapps.HotelOrderSystem.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class OrderDto(
    val orderId: Int,
    val roomId: Int,
    val roomNumber: String,
    val assignedTeamId: Int? = null,
    val assignedTeamName: String? = null,
    val source: String,
    val status: String,
    val createdByUserId: Int? = null,
    val createdByUserName: String? = null,
    val acceptedByUserId: Int? = null,
    val acceptedByUserName: String? = null,
    val createdAtUtc: String,
    val acceptedAtUtc: String? = null,
    val completedAtUtc: String? = null,
    val slaDueAtUtc: String? = null,
    val escalatedAtUtc: String? = null,
    val rowVersion: String,
    val details: List<OrderDetailDto> = emptyList()
)

@Serializable
data class OrderDetailDto(
    val orderDetailId: Int,
    val itemId: Int,
    val itemName: String,
    val quantity: Int,
    val dynamicAttributes: String = "{}"
)

@Serializable
data class AcceptOrderRequestDto(
    val rowVersion: String? = null
)

@Serializable
data class CompleteOrderRequestDto(
    val notes: String? = null
)

@Serializable
data class CancelOrderRequestDto(
    val reason: String? = null
)
