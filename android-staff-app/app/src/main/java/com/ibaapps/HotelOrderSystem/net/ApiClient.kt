package com.ibaapps.HotelOrderSystem.net

import com.ibaapps.HotelOrderSystem.BuildConfig
import com.ibaapps.HotelOrderSystem.storage.AppPrefs
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL

class ApiClient(private val prefs: AppPrefs) {
    suspend fun registerDeviceToken(fcmToken: String) = put("/api/v1/auth/device-token", JSONObject().apply {
        put("deviceId", prefs.deviceId)
        put("platform", "Android")
        put("appVersion", BuildConfig.VERSION_NAME)
        put("fcmToken", fcmToken)
    })

    suspend fun setAvailability(isReady: Boolean) = put("/api/v1/presence/availability", JSONObject().apply {
        put("isReady", isReady)
        put("deviceId", prefs.deviceId)
        put("source", "Android")
    })

    suspend fun heartbeat(appState: String, currentScreen: String) = put("/api/v1/presence/heartbeat", JSONObject().apply {
        put("deviceId", prefs.deviceId)
        put("appState", appState)
        put("currentScreen", currentScreen)
    })

    private suspend fun put(path: String, body: JSONObject): JSONObject = request("PUT", path, body)

    private suspend fun request(method: String, path: String, body: JSONObject): JSONObject = withContext(Dispatchers.IO) {
        val token = prefs.authToken ?: throw IllegalStateException("No auth token stored yet")
        val url = URL(BuildConfig.API_BASE_URL.trimEnd('/') + path)
        val conn = (url.openConnection() as HttpURLConnection).apply {
            requestMethod = method
            connectTimeout = 15000
            readTimeout = 15000
            doOutput = true
            setRequestProperty("Accept", "application/json")
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("Authorization", "Bearer $token")
        }

        OutputStreamWriter(conn.outputStream, Charsets.UTF_8).use { it.write(body.toString()) }
        val code = conn.responseCode
        val stream = if (code in 200..299) conn.inputStream else conn.errorStream
        val text = stream?.bufferedReader()?.use { it.readText() }.orEmpty()
        if (code !in 200..299) throw IllegalStateException(text.ifBlank { "HTTP $code" })
        JSONObject(text)
    }
}
