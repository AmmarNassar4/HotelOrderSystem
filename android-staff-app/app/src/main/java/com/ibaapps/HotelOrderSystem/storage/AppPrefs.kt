package com.ibaapps.HotelOrderSystem.storage

import android.content.Context
import androidx.core.content.edit

class AppPrefs(context: Context) {
    private val prefs = context.getSharedPreferences("hotel_staff_app", Context.MODE_PRIVATE)

    var authToken: String?
        get() = prefs.getString(KEY_AUTH_TOKEN, null)
        set(value) = prefs.edit { putString(KEY_AUTH_TOKEN, value) }

    var deviceId: String
        get() {
            val existing = prefs.getString(KEY_DEVICE_ID, null)
            if (!existing.isNullOrBlank()) return existing
            val created = "android-" + java.util.UUID.randomUUID().toString().replace("-", "")
            prefs.edit { putString(KEY_DEVICE_ID, created) }
            return created
        }
        set(value) = prefs.edit { putString(KEY_DEVICE_ID, value) }

    var isReady: Boolean
        get() = prefs.getBoolean(KEY_IS_READY, false)
        set(value) = prefs.edit { putBoolean(KEY_IS_READY, value) }

    var fcmToken: String?
        get() = prefs.getString(KEY_FCM_TOKEN, null)
        set(value) = prefs.edit { putString(KEY_FCM_TOKEN, value) }

    companion object {
        private const val KEY_AUTH_TOKEN = "auth_token"
        private const val KEY_DEVICE_ID = "device_id"
        private const val KEY_IS_READY = "is_ready"
        private const val KEY_FCM_TOKEN = "fcm_token"
    }
}
