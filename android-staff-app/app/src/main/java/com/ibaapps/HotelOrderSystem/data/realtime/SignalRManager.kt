package com.ibaapps.HotelOrderSystem.data.realtime

import android.util.Log
import com.ibaapps.HotelOrderSystem.BuildConfig
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeConnectionState
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeEvent
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton

/** Gson-deserialized shape of the OrderDto args sent by the staff hub. */
internal class SignalROrderPayload {
    var orderId: Int = 0
    var status: String? = null
}

/**
 * SignalR staff-hub client. Connects to `/hubs/staff?access_token=<jwt>` and
 * re-emits order events. Reconnects automatically while the session is valid;
 * exposes a [RealtimeConnectionState] for a "Reconnecting…" UI (§8).
 */
@Singleton
class SignalRManager @Inject constructor(
    private val session: SessionManager
) : RealtimeService {

    private val scope = CoroutineScope(SupervisorJob())

    private val _connectionState = MutableStateFlow(RealtimeConnectionState.Disconnected)
    override val connectionState: StateFlow<RealtimeConnectionState> = _connectionState.asStateFlow()

    private val _events = MutableSharedFlow<RealtimeEvent>(extraBufferCapacity = 32)
    override val events: SharedFlow<RealtimeEvent> = _events.asSharedFlow()

    @Volatile
    private var running = false
    private var loopJob: Job? = null
    private var connection: HubConnection? = null

    override fun start() {
        if (running) return
        running = true
        loopJob = scope.launch { connectLoop() }
    }

    override fun stop() {
        running = false
        loopJob?.cancel()
        loopJob = null
        val conn = connection
        connection = null
        _connectionState.value = RealtimeConnectionState.Disconnected
        scope.launch { runCatching { conn?.stop()?.blockingAwait(5, TimeUnit.SECONDS) } }
    }

    private suspend fun connectLoop() {
        while (running && session.isLoggedIn()) {
            val token = session.authToken
            if (token.isNullOrBlank()) break

            val conn = buildConnection(token)
            connection = conn
            _connectionState.value = RealtimeConnectionState.Connecting

            val connected = runCatching { conn.start().blockingAwait(20, TimeUnit.SECONDS) }
                .getOrElse { e ->
                    Log.w(TAG, "SignalR connect failed: ${e.message}")
                    false
                }

            if (connected) {
                _connectionState.value = RealtimeConnectionState.Connected
                // Hold while connected; break out when the socket drops.
                while (running && session.isLoggedIn() && conn.connectionState == HubConnectionState.CONNECTED) {
                    delay(POLL_MS)
                }
            }

            _connectionState.value = RealtimeConnectionState.Disconnected
            runCatching { conn.stop().blockingAwait(5, TimeUnit.SECONDS) }

            if (running && session.isLoggedIn()) delay(RECONNECT_DELAY_MS)
        }
        running = false
        _connectionState.value = RealtimeConnectionState.Disconnected
    }

    private fun buildConnection(token: String): HubConnection {
        val url = BuildConfig.API_BASE_URL.trimEnd('/') + BuildConfig.SIGNALR_STAFF_HUB + "?access_token=$token"
        val conn = HubConnectionBuilder.create(url).build()
        registerHandler(conn, RealtimeEvent.ORDER_CREATED)
        registerHandler(conn, RealtimeEvent.ORDER_ACCEPTED)
        registerHandler(conn, RealtimeEvent.ORDER_COMPLETED)
        return conn
    }

    private fun registerHandler(conn: HubConnection, eventType: String) {
        conn.on(
            eventType,
            { payload -> _events.tryEmit(RealtimeEvent(eventType, payload.orderId.takeIf { it > 0 })) },
            SignalROrderPayload::class.java
        )
    }

    private companion object {
        const val TAG = "SignalRManager"
        const val POLL_MS = 3_000L
        const val RECONNECT_DELAY_MS = 4_000L
    }
}
