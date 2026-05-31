package com.ibaapps.HotelOrderSystem.domain.repository

import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.AvailabilityStatus
import com.ibaapps.HotelOrderSystem.domain.model.HeartbeatStatus
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile

interface AuthRepository {
    /** Authenticates and, on success, persists the session. Returns the user profile. */
    suspend fun login(userName: String, password: String): NetworkResult<UserProfile>

    /** Calls the backend logout endpoint (deactivates the device token). */
    suspend fun logout(): NetworkResult<Unit>
}

interface DeviceRepository {
    /** Registers/refreshes this device's FCM token with the backend. */
    suspend fun registerToken(fcmToken: String): NetworkResult<Unit>
}

interface OrderRepository {
    suspend fun getPending(): NetworkResult<List<Order>>
    suspend fun getMyActive(): NetworkResult<List<Order>>
    suspend fun getOrder(orderId: Int): NetworkResult<Order>
    suspend fun accept(orderId: Int, rowVersion: String?): NetworkResult<Order>
    suspend fun complete(orderId: Int, notes: String?): NetworkResult<Order>
    suspend fun cancel(orderId: Int, reason: String?): NetworkResult<Order>
}

interface PresenceRepository {
    suspend fun heartbeat(appState: String, currentScreen: String?): NetworkResult<HeartbeatStatus>
    suspend fun setAvailability(isReady: Boolean): NetworkResult<AvailabilityStatus>
}
