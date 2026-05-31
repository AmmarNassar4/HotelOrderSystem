package com.ibaapps.HotelOrderSystem.domain.realtime

import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

enum class RealtimeConnectionState { Disconnected, Connecting, Connected }

/** A realtime order event received from the staff hub. */
data class RealtimeEvent(val type: String, val orderId: Int?) {
    companion object {
        const val ORDER_CREATED = "OrderCreated"
        const val ORDER_ACCEPTED = "OrderAccepted"
        const val ORDER_COMPLETED = "OrderCompleted"
    }
}

/**
 * Abstraction over the realtime (SignalR) connection so ViewModels can react to
 * live order events and connection state without depending on the transport.
 */
interface RealtimeService {
    val connectionState: StateFlow<RealtimeConnectionState>
    val events: SharedFlow<RealtimeEvent>
    fun start()
    fun stop()
}
