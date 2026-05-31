package com.ibaapps.HotelOrderSystem.data.remote.mapper

import com.ibaapps.HotelOrderSystem.data.remote.dto.AvailabilityResponseDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.HeartbeatResponseDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.UserProfileDto
import com.ibaapps.HotelOrderSystem.domain.model.AvailabilityStatus
import com.ibaapps.HotelOrderSystem.domain.model.HeartbeatStatus
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile

fun UserProfileDto.toDomain(): UserProfile = UserProfile(
    userId = userId,
    fullName = fullName,
    userName = userName,
    role = role,
    teamId = teamId,
    teamName = teamName
)

fun HeartbeatResponseDto.toDomain(): HeartbeatStatus = HeartbeatStatus(
    online = online,
    isReady = isReady,
    pendingOrdersCount = pendingOrdersCount,
    serverTime = serverTimeUtc.toUtcInstantOrNull()
)

fun AvailabilityResponseDto.toDomain(): AvailabilityStatus = AvailabilityStatus(
    isReady = isReady,
    readySince = readySinceAtUtc.toUtcInstantOrNull(),
    changedAt = changedAtUtc.toUtcInstantOrNull()
)
