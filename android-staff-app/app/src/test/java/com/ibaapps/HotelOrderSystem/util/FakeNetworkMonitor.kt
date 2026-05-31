package com.ibaapps.HotelOrderSystem.util

import com.ibaapps.HotelOrderSystem.monitor.NetworkMonitor
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

class FakeNetworkMonitor(online: Boolean = true) : NetworkMonitor {
    private val _isOnline = MutableStateFlow(online)
    override val isOnline: StateFlow<Boolean> = _isOnline
    fun setOnline(value: Boolean) { _isOnline.value = value }
}
