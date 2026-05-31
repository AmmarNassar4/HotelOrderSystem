package com.ibaapps.HotelOrderSystem.data.remote.dto

import kotlinx.serialization.Serializable

/**
 * Standard backend response envelope: `{ isSuccess, data, errorMessage }`.
 */
@Serializable
data class ApiEnvelope<T>(
    val isSuccess: Boolean = false,
    val data: T? = null,
    val errorMessage: String? = null
)

/** Generic acknowledgement body for action endpoints (device-token, logout). */
@Serializable
data class AckDto(
    val registered: Boolean? = null,
    val loggedOut: Boolean? = null
)
