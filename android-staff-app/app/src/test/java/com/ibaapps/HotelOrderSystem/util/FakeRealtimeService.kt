package com.ibaapps.HotelOrderSystem.util

import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeConnectionState
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeEvent
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

/** No-op realtime service for unit tests; can emit events on demand. */
class FakeRealtimeService : RealtimeService {
    private val _connectionState = MutableStateFlow(RealtimeConnectionState.Disconnected)
    override val connectionState: StateFlow<RealtimeConnectionState> = _connectionState

    val emitter = MutableSharedFlow<RealtimeEvent>(extraBufferCapacity = 16)
    override val events: SharedFlow<RealtimeEvent> = emitter

    var started = false
    override fun start() { started = true }
    override fun stop() { started = false }
}
