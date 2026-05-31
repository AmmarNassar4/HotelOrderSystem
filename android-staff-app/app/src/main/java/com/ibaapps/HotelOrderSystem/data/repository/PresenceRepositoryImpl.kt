package com.ibaapps.HotelOrderSystem.data.repository

import com.ibaapps.HotelOrderSystem.BuildConfig
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.data.remote.StaffApiService
import com.ibaapps.HotelOrderSystem.data.remote.dto.AvailabilityRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.HeartbeatRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.map
import com.ibaapps.HotelOrderSystem.data.remote.mapper.toDomain
import com.ibaapps.HotelOrderSystem.data.remote.safeApiCall
import com.ibaapps.HotelOrderSystem.domain.model.AvailabilityStatus
import com.ibaapps.HotelOrderSystem.domain.model.HeartbeatStatus
import com.ibaapps.HotelOrderSystem.domain.repository.PresenceRepository
import kotlinx.serialization.json.Json
import javax.inject.Inject

class PresenceRepositoryImpl @Inject constructor(
    private val api: StaffApiService,
    private val session: SessionManager,
    private val json: Json
) : PresenceRepository {

    override suspend fun heartbeat(appState: String, currentScreen: String?): NetworkResult<HeartbeatStatus> {
        val result = safeApiCall(json) {
            api.heartbeat(HeartbeatRequestDto(session.deviceId, appState, currentScreen))
        }.map { it.toDomain() }
        if (result is NetworkResult.Success) session.isReady = result.data.isReady
        return result
    }

    override suspend fun setAvailability(isReady: Boolean): NetworkResult<AvailabilityStatus> {
        val result = safeApiCall(json) {
            api.setAvailability(AvailabilityRequestDto(isReady, session.deviceId, BuildConfig.PLATFORM))
        }.map { it.toDomain() }
        if (result is NetworkResult.Success) session.isReady = result.data.isReady
        return result
    }
}
