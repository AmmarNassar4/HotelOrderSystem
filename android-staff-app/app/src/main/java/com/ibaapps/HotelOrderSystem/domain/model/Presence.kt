package com.ibaapps.HotelOrderSystem.domain.model

import java.time.Instant

data class HeartbeatStatus(
    val online: Boolean,
    val isReady: Boolean,
    val pendingOrdersCount: Int,
    val serverTime: Instant?
)

data class AvailabilityStatus(
    val isReady: Boolean,
    val readySince: Instant?,
    val changedAt: Instant?
)
