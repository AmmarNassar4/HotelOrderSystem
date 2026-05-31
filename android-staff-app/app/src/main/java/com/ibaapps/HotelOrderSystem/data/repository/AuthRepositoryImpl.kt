package com.ibaapps.HotelOrderSystem.data.repository

import com.ibaapps.HotelOrderSystem.BuildConfig
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.data.remote.StaffApiService
import com.ibaapps.HotelOrderSystem.data.remote.dto.DeviceTokenRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.LoginRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.LogoutRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.map
import com.ibaapps.HotelOrderSystem.data.remote.mapper.toDomain
import com.ibaapps.HotelOrderSystem.data.remote.mapper.toEpochMillis
import com.ibaapps.HotelOrderSystem.data.remote.safeApiCall
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import com.ibaapps.HotelOrderSystem.domain.repository.AuthRepository
import com.ibaapps.HotelOrderSystem.domain.repository.DeviceRepository
import kotlinx.serialization.json.Json
import javax.inject.Inject

class AuthRepositoryImpl @Inject constructor(
    private val api: StaffApiService,
    private val session: SessionManager,
    private val json: Json
) : AuthRepository {

    override suspend fun login(userName: String, password: String): NetworkResult<UserProfile> {
        val result = safeApiCall(json) { api.login(LoginRequestDto(userName, password)) }
        return when (result) {
            is NetworkResult.Success -> {
                val dto = result.data
                val profile = dto.user.toDomain()
                session.saveSession(dto.token, dto.expiresAtUtc.toEpochMillis(), profile)
                NetworkResult.Success(profile)
            }
            is NetworkResult.Error -> result
        }
    }

    override suspend fun logout(): NetworkResult<Unit> =
        safeApiCall(json) { api.logout(LogoutRequestDto(session.deviceId)) }.map { }
}

class DeviceRepositoryImpl @Inject constructor(
    private val api: StaffApiService,
    private val session: SessionManager,
    private val json: Json
) : DeviceRepository {

    override suspend fun registerToken(fcmToken: String): NetworkResult<Unit> {
        session.fcmToken = fcmToken
        return safeApiCall(json) {
            api.registerDevice(
                DeviceTokenRequestDto(
                    deviceId = session.deviceId,
                    platform = BuildConfig.PLATFORM,
                    appVersion = BuildConfig.VERSION_NAME,
                    fcmToken = fcmToken
                )
            )
        }.map { }
    }
}
