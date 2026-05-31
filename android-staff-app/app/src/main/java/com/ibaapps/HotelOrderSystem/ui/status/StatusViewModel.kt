package com.ibaapps.HotelOrderSystem.ui.status

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.repository.PresenceRepository
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import com.ibaapps.HotelOrderSystem.ui.common.toUserMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.Instant
import javax.inject.Inject

data class StatusUiState(
    val isReady: Boolean = false,
    val online: Boolean = false,
    val pendingOrdersCount: Int = 0,
    val lastHeartbeatAt: Instant? = null,
    val teamName: String? = null,
    val role: String? = null,
    val isToggling: Boolean = false,
    val errorMessage: String? = null
)

private data class LocalStatus(val isToggling: Boolean = false, val errorMessage: String? = null)

@HiltViewModel
class StatusViewModel @Inject constructor(
    private val presenceRepository: PresenceRepository,
    private val presenceManager: PresenceManager,
    session: SessionManager
) : ViewModel() {

    private val _local = MutableStateFlow(LocalStatus())
    private val teamName = session.profile?.teamName
    private val role = session.profile?.role

    val state: StateFlow<StatusUiState> =
        combine(presenceManager.state, _local) { presence, local ->
            StatusUiState(
                isReady = presence.isReady,
                online = presence.online,
                pendingOrdersCount = presence.pendingOrdersCount,
                lastHeartbeatAt = presence.lastHeartbeatAt,
                teamName = teamName,
                role = role,
                isToggling = local.isToggling,
                errorMessage = local.errorMessage
            )
        }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), StatusUiState(teamName = teamName, role = role))

    fun setReady(target: Boolean) {
        // Optimistic: reflect immediately, then confirm with the server.
        presenceManager.updateReady(target)
        _local.update { it.copy(isToggling = true, errorMessage = null) }
        viewModelScope.launch {
            when (val result = presenceRepository.setAvailability(target)) {
                is NetworkResult.Success -> {
                    presenceManager.updateReady(result.data.isReady)
                    _local.update { it.copy(isToggling = false) }
                }
                is NetworkResult.Error -> {
                    presenceManager.updateReady(!target) // revert
                    _local.update { it.copy(isToggling = false, errorMessage = result.toUserMessage()) }
                }
            }
        }
    }

    fun clearError() = _local.update { it.copy(errorMessage = null) }
}
