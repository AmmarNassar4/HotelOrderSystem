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
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.domain.repository.DeviceRepository
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import javax.inject.Inject

@AndroidEntryPoint
class HotelFirebaseMessagingService : FirebaseMessagingService() {

    @Inject
    lateinit var deviceRepository: DeviceRepository

    @Inject
    lateinit var session: SessionManager

    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        session.fcmToken = token
        if (session.isLoggedIn()) {
            scope.launch { runCatching { deviceRepository.registerToken(token) } }
        }
    }

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)
        if (!session.isLoggedIn()) return

        val data = message.data
        val orderId = parseOrderIdFromData(data)
        val type = data["type"] ?: data["notificationType"]
        val title = message.notification?.title ?: notificationTitleFor(type)
        val body = message.notification?.body
            ?: data["body"]
            ?: data["roomNumber"]?.let { "Room $it" }
            ?: "Tap to open staff orders"

        showNotification(title, body, orderId)
    }

    private fun showNotification(title: String, body: String, orderId: Int?) {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_SINGLE_TOP
            if (orderId != null) putExtra(EXTRA_ORDER_ID, orderId)
        }
        val pendingIntent = PendingIntent.getActivity(
            this,
            orderId ?: 0,
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

        // Dedupe: use the order id as the notification id so repeated events for
        // the same order replace rather than stack (§19 "no duplicate notifications").
        val notificationId = orderId ?: DEFAULT_NOTIFICATION_ID
        NotificationManagerCompat.from(this).notify(notificationId, notification)
    }

    companion object {
        const val EXTRA_ORDER_ID = "extra_order_id"
        private const val DEFAULT_NOTIFICATION_ID = 1000
    }
}
