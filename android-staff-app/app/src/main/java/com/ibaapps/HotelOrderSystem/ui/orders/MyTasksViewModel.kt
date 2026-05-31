package com.ibaapps.HotelOrderSystem.ui.orders

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeEvent
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
import com.ibaapps.HotelOrderSystem.ui.common.toUserMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class MyTasksUiState(
    val orders: List<Order> = emptyList(),
    val isLoading: Boolean = false,
    val isRefreshing: Boolean = false,
    val completingIds: Set<Int> = emptySet(),
    val errorMessage: String? = null,
    val transientMessage: String? = null,
    val loadedOnce: Boolean = false
)

@HiltViewModel
class MyTasksViewModel @Inject constructor(
    private val orderRepository: OrderRepository,
    realtimeService: RealtimeService
) : ViewModel() {

    private val _state = MutableStateFlow(MyTasksUiState())
    val state: StateFlow<MyTasksUiState> = _state.asStateFlow()

    init {
        load()
        // Live updates: my active list changes when orders are accepted or completed.
        viewModelScope.launch {
            realtimeService.events.collect { event ->
                when (event.type) {
                    RealtimeEvent.ORDER_ACCEPTED, RealtimeEvent.ORDER_COMPLETED -> fetch()
                }
            }
        }
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
            when (val result = orderRepository.getMyActive()) {
                is NetworkResult.Success -> _state.update {
                    it.copy(orders = result.data, isLoading = false, isRefreshing = false, loadedOnce = true, errorMessage = null)
                }
                is NetworkResult.Error -> _state.update {
                    it.copy(isLoading = false, isRefreshing = false, loadedOnce = true, errorMessage = result.toUserMessage())
                }
            }
        }
    }

    fun complete(orderId: Int, notes: String?) {
        if (orderId in _state.value.completingIds) return
        _state.update { it.copy(completingIds = it.completingIds + orderId) }
        viewModelScope.launch {
            when (val result = orderRepository.complete(orderId, notes?.takeIf { it.isNotBlank() })) {
                is NetworkResult.Success -> _state.update {
                    it.copy(
                        orders = it.orders.filterNot { o -> o.orderId == orderId },
                        completingIds = it.completingIds - orderId,
                        transientMessage = "Order completed"
                    )
                }
                is NetworkResult.Error -> _state.update {
                    it.copy(completingIds = it.completingIds - orderId, transientMessage = result.toUserMessage())
                }
            }
        }
    }

    fun consumeTransientMessage() = _state.update { it.copy(transientMessage = null) }
}
