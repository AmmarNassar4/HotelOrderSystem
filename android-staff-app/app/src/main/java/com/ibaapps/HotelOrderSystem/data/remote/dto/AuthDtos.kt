package com.ibaapps.HotelOrderSystem.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class LoginRequestDto(
    val userName: String,
    val password: String
)

@Serializable
data class AuthResponseDto(
    val token: String,
    val expiresAtUtc: String,
    val user: UserProfileDto
)

@Serializable
data class UserProfileDto(
    val userId: Int,
    val fullName: String,
    val userName: String,
    val role: String,
    val teamId: Int? = null,
    val teamName: String? = null
)

@Serializable
data class DeviceTokenRequestDto(
    val deviceId: String,
    val platform: String,
    val appVersion: String,
    val fcmToken: String
)

@Serializable
data class LogoutRequestDto(
    val deviceId: String
)
