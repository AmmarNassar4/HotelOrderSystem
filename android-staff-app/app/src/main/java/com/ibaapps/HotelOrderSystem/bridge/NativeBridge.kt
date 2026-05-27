package com.ibaapps.HotelOrderSystem.bridge

import android.webkit.JavascriptInterface
import com.ibaapps.HotelOrderSystem.net.ApiClient
import com.ibaapps.HotelOrderSystem.storage.AppPrefs
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class NativeBridge(
    private val prefs: AppPrefs,
    private val api: ApiClient,
    private val scope: CoroutineScope,
    private val onReadyChanged: (Boolean) -> Unit
) {
    @JavascriptInterface
    fun setAuthToken(token: String?) {
        val clean = token?.trim()?.takeIf { it.isNotBlank() }
        prefs.authToken = clean
        if (clean != null) {
            prefs.fcmToken?.let { fcm ->
                scope.launch(Dispatchers.IO) { runCatching { api.registerDeviceToken(fcm) } }
            }
        }
    }

    @JavascriptInterface
    fun setReady(value: Boolean) {
        prefs.isReady = value
        onReadyChanged(value)
        scope.launch(Dispatchers.IO) {
            runCatching { api.setAvailability(value) }
        }
    }

    @JavascriptInterface
    fun getDeviceId(): String = prefs.deviceId

    @JavascriptInterface
    fun isReady(): Boolean = prefs.isReady
}
