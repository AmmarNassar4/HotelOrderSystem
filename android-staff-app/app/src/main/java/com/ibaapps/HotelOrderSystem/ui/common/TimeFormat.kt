package com.ibaapps.HotelOrderSystem.ui.common

import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val clockFormatter: DateTimeFormatter =
    DateTimeFormatter.ofPattern("HH:mm:ss").withZone(ZoneId.systemDefault())

private val dateTimeFormatter: DateTimeFormatter =
    DateTimeFormatter.ofPattern("MMM d, HH:mm").withZone(ZoneId.systemDefault())

fun Instant?.toClockTimeOrDash(): String = this?.let { clockFormatter.format(it) } ?: "—"

fun Instant?.toDateTimeOrDash(): String = this?.let { dateTimeFormatter.format(it) } ?: "—"

/** Compact "Xm ago" / "Xh ago" relative label for list timestamps. */
fun Instant?.toRelativeAgo(now: Instant = Instant.now()): String {
    if (this == null) return "—"
    val seconds = (now.epochSecond - epochSecond).coerceAtLeast(0)
    return when {
        seconds < 60 -> "just now"
        seconds < 3600 -> "${seconds / 60}m ago"
        seconds < 86_400 -> "${seconds / 3600}h ago"
        else -> "${seconds / 86_400}d ago"
    }
}
