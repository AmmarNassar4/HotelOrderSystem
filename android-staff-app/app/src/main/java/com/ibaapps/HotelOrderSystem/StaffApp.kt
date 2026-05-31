package com.ibaapps.HotelOrderSystem

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import androidx.lifecycle.ProcessLifecycleOwner
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import dagger.hilt.android.HiltAndroidApp
import javax.inject.Inject

@HiltAndroidApp
class StaffApp : Application() {

    @Inject
    lateinit var presenceManager: PresenceManager

    override fun onCreate() {
        super.onCreate()
        // Drive heartbeats from the process foreground/background lifecycle.
        ProcessLifecycleOwner.get().lifecycle.addObserver(presenceManager)
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
