package com.ibaapps.HotelOrderSystem.ui.main

import androidx.lifecycle.ViewModel
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeConnectionState
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import com.ibaapps.HotelOrderSystem.presence.PresenceUiState
import com.ibaapps.HotelOrderSystem.push.FcmTokenRegistrar
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.StateFlow
import javax.inject.Inject

/**
 * Owns the authenticated home. Kicks the heartbeat loop alive once the user
 * reaches the main scaffold (login leaves us already foregrounded, so the
 * process lifecycle won't re-fire ON_START on its own).
 */
@HiltViewModel
class MainViewModel @Inject constructor(
    private val presenceManager: PresenceManager,
    private val fcmTokenRegistrar: FcmTokenRegistrar,
    private val realtimeService: RealtimeService
) : ViewModel() {

    val presenceState: StateFlow<PresenceUiState> = presenceManager.state
    val connectionState: StateFlow<RealtimeConnectionState> = realtimeService.connectionState

    init {
        presenceManager.start()
        realtimeService.start()
        // Register/refresh the FCM token for this device on entering the
        // authenticated home (covers fresh login and reinstall).
        fcmTokenRegistrar.register()
    }

    fun setCurrentScreen(screen: String) {
        presenceManager.currentScreen = screen
    }
}
