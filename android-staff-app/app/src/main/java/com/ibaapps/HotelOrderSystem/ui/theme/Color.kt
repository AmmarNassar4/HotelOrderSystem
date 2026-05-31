package com.ibaapps.HotelOrderSystem.ui.theme

import androidx.compose.ui.graphics.Color

// Brand palette
val BluePrimary = Color(0xFF2563EB)
val BluePrimaryDark = Color(0xFF1D4ED8)
val SlateDark = Color(0xFF0F172A)
val SlateSurface = Color(0xFFF8FAFC)
val SlateOnSurface = Color(0xFF0F172A)

// Status colors (requirements doc §10)
val StatusPending = Color(0xFF2563EB)      // blue / neutral
val StatusInProgress = Color(0xFFF59E0B)   // amber (accepted / in progress)
val StatusCompleted = Color(0xFF16A34A)    // green
val StatusCancelled = Color(0xFF6B7280)    // gray
val StatusUrgent = Color(0xFFDC2626)       // red (SLA breach / escalated)

val ReadyGreen = Color(0xFF16A34A)
val NotReadyOrange = Color(0xFFF97316)
val OfflineRed = Color(0xFFDC2626)
