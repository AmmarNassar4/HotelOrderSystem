package com.ibaapps.HotelOrderSystem.data.local

import android.content.SharedPreferences
import androidx.core.content.edit
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.util.UUID

/**
 * Single source of truth for the device's authenticated session.
 *
 * Backed by [SharedPreferences] (an EncryptedSharedPreferences instance is
 * provided in production — see StorageModule), but the class takes a plain
 * [SharedPreferences] so it can be unit tested with any implementation.
 */
class SessionManager(private val prefs: SharedPreferences) {

    private val json = Json { ignoreUnknownKeys = true }

    var authToken: String?
        get() = prefs.getString(KEY_TOKEN, null)
        set(value) = prefs.edit { putString(KEY_TOKEN, value) }

    /** Token expiry as epoch milliseconds. 0 means unknown / not set. */
    var tokenExpiresAtEpochMs: Long
        get() = prefs.getLong(KEY_TOKEN_EXP, 0L)
        set(value) = prefs.edit { putLong(KEY_TOKEN_EXP, value) }

    var fcmToken: String?
        get() = prefs.getString(KEY_FCM_TOKEN, null)
        set(value) = prefs.edit { putString(KEY_FCM_TOKEN, value) }

    var isReady: Boolean
        get() = prefs.getBoolean(KEY_IS_READY, false)
        set(value) = prefs.edit { putBoolean(KEY_IS_READY, value) }

    /** Stable per-install device id (`android-<uuid>`), generated once and reused. */
    val deviceId: String
        get() {
            prefs.getString(KEY_DEVICE_ID, null)?.takeIf { it.isNotBlank() }?.let { return it }
            val created = "android-" + UUID.randomUUID().toString().replace("-", "")
            prefs.edit { putString(KEY_DEVICE_ID, created) }
            return created
        }

    var profile: UserProfile?
        get() = prefs.getString(KEY_PROFILE, null)
            ?.let { runCatching { json.decodeFromString<UserProfile>(it) }.getOrNull() }
        set(value) = prefs.edit {
            if (value == null) remove(KEY_PROFILE) else putString(KEY_PROFILE, json.encodeToString(value))
        }

    /** Persist a freshly issued login session atomically. */
    fun saveSession(token: String, expiresAtEpochMs: Long, profile: UserProfile) {
        prefs.edit {
            putString(KEY_TOKEN, token)
            putLong(KEY_TOKEN_EXP, expiresAtEpochMs)
            putString(KEY_PROFILE, json.encodeToString(profile))
        }
    }

    /** True when a token is present and not past its expiry. */
    fun isTokenValid(nowMs: Long = System.currentTimeMillis()): Boolean {
        if (authToken.isNullOrBlank()) return false
        val exp = tokenExpiresAtEpochMs
        return exp <= 0L || nowMs < exp
    }

    fun isLoggedIn(nowMs: Long = System.currentTimeMillis()): Boolean = isTokenValid(nowMs)

    /** Clear the authenticated session. Device id and FCM token survive logout. */
    fun clear() {
        prefs.edit {
            remove(KEY_TOKEN)
            remove(KEY_TOKEN_EXP)
            remove(KEY_PROFILE)
            remove(KEY_IS_READY)
        }
    }

    private companion object {
        const val KEY_TOKEN = "auth_token"
        const val KEY_TOKEN_EXP = "auth_token_expires_at"
        const val KEY_PROFILE = "user_profile"
        const val KEY_DEVICE_ID = "device_id"
        const val KEY_IS_READY = "is_ready"
        const val KEY_FCM_TOKEN = "fcm_token"
    }
}
