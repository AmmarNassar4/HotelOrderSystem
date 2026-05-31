package com.ibaapps.HotelOrderSystem.data.remote.mapper

import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneOffset

/**
 * Parses backend UTC timestamps. ASP.NET Core may emit either an offset/`Z`
 * form (`2026-05-31T12:00:00Z`) or an unspecified-kind form without a zone
 * (`2026-05-31T12:00:00.123`), so both are handled and treated as UTC.
 */
fun String?.toUtcInstantOrNull(): Instant? {
    if (this.isNullOrBlank()) return null
    runCatching { return Instant.parse(this) }
    return runCatching { LocalDateTime.parse(this).toInstant(ZoneOffset.UTC) }.getOrNull()
}

fun String?.toEpochMillis(default: Long = 0L): Long =
    this.toUtcInstantOrNull()?.toEpochMilli() ?: default
