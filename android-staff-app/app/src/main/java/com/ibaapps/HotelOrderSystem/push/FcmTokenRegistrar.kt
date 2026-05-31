package com.ibaapps.HotelOrderSystem.push

import com.google.firebase.messaging.FirebaseMessaging
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.domain.repository.DeviceRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Fetches the current FCM token and registers it with the backend. Called after
 * login and whenever the authenticated home appears, covering new-login and
 * app-reinstall cases (§5). Token refreshes are handled in the messaging service.
 */
@Singleton
class FcmTokenRegistrar @Inject constructor(
    private val deviceRepository: DeviceRepository,
    private val session: SessionManager
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    fun register() {
        FirebaseMessaging.getInstance().token.addOnSuccessListener { token ->
            if (token.isNullOrBlank()) return@addOnSuccessListener
            session.fcmToken = token
            if (session.isLoggedIn()) {
                scope.launch { runCatching { deviceRepository.registerToken(token) } }
            }
        }
    }
}
