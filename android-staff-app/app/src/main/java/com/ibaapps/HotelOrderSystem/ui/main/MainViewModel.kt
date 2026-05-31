package com.ibaapps.HotelOrderSystem.ui.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.AuthEventBus
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeConnectionState
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import com.ibaapps.HotelOrderSystem.monitor.NetworkMonitor
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import com.ibaapps.HotelOrderSystem.presence.PresenceUiState
import com.ibaapps.HotelOrderSystem.push.FcmTokenRegistrar
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class MainViewModel @Inject constructor(
    private val presenceManager: PresenceManager,
    private val fcmTokenRegistrar: FcmTokenRegistrar,
    private val realtimeService: RealtimeService,
    private val session: SessionManager,
    networkMonitor: NetworkMonitor,
    authEventBus: AuthEventBus
) : ViewModel() {

    val presenceState: StateFlow<PresenceUiState> = presenceManager.state
    val connectionState: StateFlow<RealtimeConnectionState> = realtimeService.connectionState
    val isOnline: StateFlow<Boolean> = networkMonitor.isOnline

    private val _forceLoggedOut = MutableStateFlow(false)
    val forceLoggedOut: StateFlow<Boolean> = _forceLoggedOut.asStateFlow()

    init {
        presenceManager.start()
        realtimeService.start()
        fcmTokenRegistrar.register()

        // A 401 on any authenticated call invalidates the session: tear down and
        // bounce to login (§15).
        viewModelScope.launch {
            authEventBus.unauthorized.collect {
                presenceManager.stop()
                realtimeService.stop()
                session.clear()
                _forceLoggedOut.value = true
            }
        }
    }

    fun setCurrentScreen(screen: String) {
        presenceManager.currentScreen = screen
    }
}
