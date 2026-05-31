package com.ibaapps.HotelOrderSystem.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.Roles
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import com.ibaapps.HotelOrderSystem.domain.repository.AuthRepository
import com.ibaapps.HotelOrderSystem.ui.common.toUserMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface LoginUiState {
    data object Idle : LoginUiState
    data object Loading : LoginUiState
    data class Error(val message: String) : LoginUiState
    data object AccessDenied : LoginUiState
    data class Success(val profile: UserProfile) : LoginUiState
}

@HiltViewModel
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val session: SessionManager
) : ViewModel() {

    private val _state = MutableStateFlow<LoginUiState>(LoginUiState.Idle)
    val state: StateFlow<LoginUiState> = _state.asStateFlow()

    fun login(userName: String, password: String) {
        val user = userName.trim()
        if (user.isBlank() || password.isBlank()) {
            _state.value = LoginUiState.Error("Enter your username and password.")
            return
        }

        _state.value = LoginUiState.Loading
        viewModelScope.launch {
            when (val result = authRepository.login(user, password)) {
                is NetworkResult.Success -> {
                    val profile = result.data
                    if (Roles.isStaffAppAllowed(profile.role)) {
                        _state.value = LoginUiState.Success(profile)
                    } else {
                        // Non-staff accounts must not stay authenticated in this app.
                        session.clear()
                        _state.value = LoginUiState.AccessDenied
                    }
                }
                is NetworkResult.Error -> {
                    _state.value = LoginUiState.Error(result.toUserMessage())
                }
            }
        }
    }

    fun reset() {
        _state.value = LoginUiState.Idle
    }
}
