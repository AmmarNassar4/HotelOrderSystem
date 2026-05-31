package com.ibaapps.HotelOrderSystem.ui.orders

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
import com.ibaapps.HotelOrderSystem.ui.common.toUserMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class PendingUiState(
    val orders: List<Order> = emptyList(),
    val isLoading: Boolean = false,
    val isRefreshing: Boolean = false,
    val acceptingIds: Set<Int> = emptySet(),
    val errorMessage: String? = null,
    val transientMessage: String? = null,
    val loadedOnce: Boolean = false
)

@HiltViewModel
class PendingViewModel @Inject constructor(
    private val orderRepository: OrderRepository
) : ViewModel() {

    private val _state = MutableStateFlow(PendingUiState())
    val state: StateFlow<PendingUiState> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = !it.loadedOnce, errorMessage = null) }
        fetch()
    }

    fun refresh() {
        _state.update { it.copy(isRefreshing = true, errorMessage = null) }
        fetch()
    }

    private fun fetch() {
        viewModelScope.launch {
            when (val result = orderRepository.getPending()) {
                is NetworkResult.Success -> _state.update {
                    it.copy(
                        orders = result.data,
                        isLoading = false,
                        isRefreshing = false,
                        loadedOnce = true,
                        errorMessage = null
                    )
                }
                is NetworkResult.Error -> _state.update {
                    it.copy(
                        isLoading = false,
                        isRefreshing = false,
                        loadedOnce = true,
                        errorMessage = result.toUserMessage()
                    )
                }
            }
        }
    }

    fun accept(order: Order) {
        if (order.orderId in _state.value.acceptingIds) return
        _state.update { it.copy(acceptingIds = it.acceptingIds + order.orderId) }
        viewModelScope.launch {
            when (val result = orderRepository.accept(order.orderId, order.rowVersion)) {
                is NetworkResult.Success -> {
                    // Success: remove from pending; it now lives under My Tasks.
                    _state.update {
                        it.copy(
                            orders = it.orders.filterNot { o -> o.orderId == order.orderId },
                            acceptingIds = it.acceptingIds - order.orderId,
                            transientMessage = "Order accepted"
                        )
                    }
                }
                is NetworkResult.Error -> {
                    val message = if (result.type == ApiErrorType.Conflict) {
                        "This order was already accepted by another staff member."
                    } else {
                        result.toUserMessage()
                    }
                    _state.update { it.copy(acceptingIds = it.acceptingIds - order.orderId, transientMessage = message) }
                    // On conflict the list is stale — refresh.
                    if (result.type == ApiErrorType.Conflict) fetch()
                }
            }
        }
    }

    fun consumeTransientMessage() = _state.update { it.copy(transientMessage = null) }
}
