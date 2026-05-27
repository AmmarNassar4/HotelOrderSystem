package com.ibaapps.HotelOrderSystem

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build

class StaffApp : Application() {
    override fun onCreate() {
        super.onCreate()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                Notifications.CHANNEL_ID,
                "Hotel Orders",
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = "New hotel order notifications"
                enableVibration(true)
            }
            getSystemService(NotificationManager::class.java).createNotificationChannel(channel)
        }
    }
}

object Notifications {
    const val CHANNEL_ID = "hotel_orders"
}
