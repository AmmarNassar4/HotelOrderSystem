package com.ibaapps.HotelOrderSystem.push

import android.app.PendingIntent
import android.content.Intent
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.ibaapps.HotelOrderSystem.MainActivity
import com.ibaapps.HotelOrderSystem.Notifications
import com.ibaapps.HotelOrderSystem.R
import com.ibaapps.HotelOrderSystem.storage.AppPrefs

class HotelFirebaseMessagingService : FirebaseMessagingService() {
    override fun onNewToken(token: String) {
        super.onNewToken(token)
        // Persist the refreshed token. Re-registration with the backend is wired
        // up in Task 12 once the Retrofit-based device repository exists.
        AppPrefs(this).fcmToken = token
    }

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)
        val prefs = AppPrefs(this)
        if (!prefs.isReady) return

        val type = message.data["type"] ?: message.data["notificationType"] ?: "ORDER_EVENT"
        val title = when (type) {
            "OrderCreated" -> "New order"
            "OrderClaimed" -> "Order claimed"
            "OrderCompleted" -> "Order completed"
            else -> "Hotel order update"
        }
        val body = message.data["body"]
            ?: message.data["roomNumber"]?.let { "Room $it" }
            ?: "Tap to open staff orders"

        showNotification(title, body)
    }

    private fun showNotification(title: String, body: String) {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
        }
        val pendingIntent = PendingIntent.getActivity(
            this,
            100,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(this, Notifications.CHANNEL_ID)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(title)
            .setContentText(body)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(pendingIntent)
            .build()

        NotificationManagerCompat.from(this).notify(System.currentTimeMillis().toInt(), notification)
    }
}
