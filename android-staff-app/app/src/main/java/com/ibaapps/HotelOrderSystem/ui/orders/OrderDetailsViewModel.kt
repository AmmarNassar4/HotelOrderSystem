package com.ibaapps.HotelOrderSystem.ui.orders

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.Roles
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
import com.ibaapps.HotelOrderSystem.ui.common.toUserMessage
import com.ibaapps.HotelOrderSystem.ui.navigation.Routes
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class OrderDetailsUiState(
    val order: Order? = null,
    val isLoading: Boolean = true,
    val errorMessage: String? = null,
    val actionInProgress: Boolean = false,
    val canCancel: Boolean = false,
    val transientMessage: String? = null
)

@HiltViewModel
class OrderDetailsViewModel @Inject constructor(
    private val orderRepository: OrderRepository,
    session: SessionManager,
    savedStateHandle: SavedStateHandle
) : ViewModel() {

    private val orderId: Int = savedStateHandle.get<Int>(Routes.ARG_ORDER_ID) ?: 0

    private val _state = MutableStateFlow(OrderDetailsUiState(canCancel = Roles.canCancelOrders(session.profile?.role)))
    val state: StateFlow<OrderDetailsUiState> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = it.order == null, errorMessage = null) }
        viewModelScope.launch {
            when (val result = orderRepository.getOrder(orderId)) {
                is NetworkResult.Success -> _state.update { it.copy(order = result.data, isLoading = false, errorMessage = null) }
                is NetworkResult.Error -> _state.update { it.copy(isLoading = false, errorMessage = result.toUserMessage()) }
            }
        }
    }

    fun accept() = runAction(conflictMessage = "This order was already accepted by another staff member.") {
        orderRepository.accept(orderId, _state.value.order?.rowVersion)
    }

    fun complete(notes: String?) = runAction {
        orderRepository.complete(orderId, notes?.takeIf { it.isNotBlank() })
    }

    fun cancel(reason: String?) = runAction {
        orderRepository.cancel(orderId, reason?.takeIf { it.isNotBlank() })
    }

    private fun runAction(conflictMessage: String? = null, call: suspend () -> NetworkResult<Order>) {
        if (_state.value.actionInProgress) return
        _state.update { it.copy(actionInProgress = true) }
        viewModelScope.launch {
            when (val result = call()) {
                is NetworkResult.Success ->
                    _state.update { it.copy(order = result.data, actionInProgress = false) }
                is NetworkResult.Error -> {
                    val message = if (result.type == ApiErrorType.Conflict && conflictMessage != null) {
                        conflictMessage
                    } else {
                        result.toUserMessage()
                    }
                    _state.update { it.copy(actionInProgress = false, transientMessage = message) }
                    if (result.type == ApiErrorType.Conflict) load()
                }
            }
        }
    }

    fun consumeTransientMessage() = _state.update { it.copy(transientMessage = null) }
}
