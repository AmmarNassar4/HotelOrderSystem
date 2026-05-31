package com.ibaapps.HotelOrderSystem.presence

import androidx.lifecycle.DefaultLifecycleObserver
import androidx.lifecycle.LifecycleOwner
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.repository.PresenceRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.time.Instant
import javax.inject.Inject
import javax.inject.Singleton

data class PresenceUiState(
    val online: Boolean = false,
    val isReady: Boolean = false,
    val pendingOrdersCount: Int = 0,
    val lastHeartbeatAt: Instant? = null
)

/**
 * Lifecycle-aware staff presence. While the app is foregrounded and the user
 * is authenticated, a heartbeat is sent every [HEARTBEAT_INTERVAL_MS]. The loop
 * stops on background, logout, or an expired/invalid token (§6.1, §13).
 *
 * Registered as a [androidx.lifecycle.ProcessLifecycleOwner] observer in StaffApp.
 */
@Singleton
class PresenceManager @Inject constructor(
    private val presenceRepository: PresenceRepository,
    private val session: SessionManager
) : DefaultLifecycleObserver {

    private val scope = CoroutineScope(SupervisorJob())

    private val _state = MutableStateFlow(PresenceUiState(isReady = session.isReady))
    val state: StateFlow<PresenceUiState> = _state.asStateFlow()

    @Volatile
    var currentScreen: String = "staff"

    private var loopJob: Job? = null

    // --- Lifecycle callbacks (process-wide) ---

    override fun onStart(owner: LifecycleOwner) {
        // App entered foreground.
        startForegroundLoop()
    }

    override fun onStop(owner: LifecycleOwner) {
        // App entered background: stop the aggressive loop, send one final
        // best-effort background heartbeat so the server sees us cleanly leaving.
        loopJob?.cancel()
        loopJob = null
        if (session.isLoggedIn()) {
            scope.launch { runCatching { presenceRepository.heartbeat("background", currentScreen) } }
        }
    }

    /** Called after login so the loop (re)starts even though we never left foreground. */
    fun start() = startForegroundLoop()

    /** Called on logout to halt heartbeats and reset presence. */
    fun stop() {
        loopJob?.cancel()
        loopJob = null
        _state.value = PresenceUiState()
    }

    /** Reflect a local Ready toggle immediately (server sync handled elsewhere). */
    fun updateReady(isReady: Boolean) {
        _state.update { it.copy(isReady = isReady) }
    }

    private fun startForegroundLoop() {
        if (loopJob?.isActive == true) return
        loopJob = scope.launch {
            while (isActive) {
                if (!session.isLoggedIn()) {
                    _state.update { it.copy(online = false) }
                    break
                }
                when (val result = presenceRepository.heartbeat("foreground", currentScreen)) {
                    is NetworkResult.Success -> _state.update {
                        it.copy(
                            online = result.data.online,
                            isReady = result.data.isReady,
                            pendingOrdersCount = result.data.pendingOrdersCount,
                            lastHeartbeatAt = result.data.serverTime ?: Instant.now()
                        )
                    }
                    is NetworkResult.Error -> {
                        _state.update { it.copy(online = false) }
                        if (result.type == ApiErrorType.Unauthorized) break // token expired
                    }
                }
                delay(HEARTBEAT_INTERVAL_MS)
            }
        }
    }

    companion object {
        const val HEARTBEAT_INTERVAL_MS = 60_000L
    }
}
