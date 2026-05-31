package com.ibaapps.HotelOrderSystem.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class HeartbeatRequestDto(
    val deviceId: String,
    val appState: String,
    val currentScreen: String? = null
)

@Serializable
data class HeartbeatResponseDto(
    val serverTimeUtc: String,
    val online: Boolean,
    val isReady: Boolean,
    val pendingOrdersCount: Int
)

@Serializable
data class AvailabilityRequestDto(
    val isReady: Boolean,
    val deviceId: String? = null,
    val source: String? = null
)

@Serializable
data class AvailabilityResponseDto(
    val isReady: Boolean,
    val readySinceAtUtc: String? = null,
    val changedAtUtc: String
)
