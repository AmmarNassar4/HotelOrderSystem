package com.ibaapps.HotelOrderSystem.ui.profile

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.BuildConfig
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import com.ibaapps.HotelOrderSystem.domain.repository.AuthRepository
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class ProfileUiState(
    val profile: UserProfile? = null,
    val appVersion: String = BuildConfig.VERSION_NAME,
    val isLoggingOut: Boolean = false,
    val loggedOut: Boolean = false
)

@HiltViewModel
class ProfileViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val session: SessionManager,
    private val presenceManager: PresenceManager,
    private val realtimeService: RealtimeService
) : ViewModel() {

    private val _state = MutableStateFlow(ProfileUiState(profile = session.profile))
    val state: StateFlow<ProfileUiState> = _state.asStateFlow()

    fun logout() {
        if (_state.value.isLoggingOut) return
        _state.update { it.copy(isLoggingOut = true) }
        viewModelScope.launch {
            // Backend logout first (deactivates the device token) while the JWT is
            // still present, then tear down local session and live connections (§4.2).
            runCatching { authRepository.logout() }
            presenceManager.stop()
            realtimeService.stop()
            session.clear()
            _state.update { it.copy(isLoggingOut = false, loggedOut = true) }
        }
    }

    private inline fun MutableStateFlow<ProfileUiState>.update(block: (ProfileUiState) -> ProfileUiState) {
        value = block(value)
    }
}
