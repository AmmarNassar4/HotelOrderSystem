package com.ibaapps.HotelOrderSystem.data.remote

import com.ibaapps.HotelOrderSystem.data.remote.dto.ApiEnvelope
import kotlinx.serialization.json.Json
import retrofit2.Response
import java.io.IOException

/** Coarse classification of a failed API call, used to drive UI/navigation. */
enum class ApiErrorType {
    Unauthorized,   // 401 -> force logout
    Forbidden,      // 403 -> access denied
    Conflict,       // 409 -> already changed, refresh
    NotFound,       // 404
    BadRequest,     // 400 / business rule failure
    Server,         // 5xx
    Offline,        // no network / IO failure
    EmptyBody,      // success status but no payload
    Unknown
}

sealed interface NetworkResult<out T> {
    data class Success<T>(val data: T) : NetworkResult<T>
    data class Error(
        val type: ApiErrorType,
        val message: String? = null,
        val httpCode: Int? = null
    ) : NetworkResult<Nothing>
}

fun mapHttpCodeToErrorType(code: Int): ApiErrorType = when (code) {
    401 -> ApiErrorType.Unauthorized
    403 -> ApiErrorType.Forbidden
    404 -> ApiErrorType.NotFound
    409 -> ApiErrorType.Conflict
    400, 422 -> ApiErrorType.BadRequest
    in 500..599 -> ApiErrorType.Server
    else -> ApiErrorType.Unknown
}

/**
 * Executes a Retrofit call that returns an [ApiEnvelope] and normalises it into a
 * [NetworkResult], mapping HTTP status codes and parsing the error envelope.
 */
suspend fun <T> safeApiCall(
    json: Json,
    apiCall: suspend () -> Response<ApiEnvelope<T>>
): NetworkResult<T> {
    return try {
        val response = apiCall()
        if (response.isSuccessful) {
            val body = response.body()
            when {
                body == null -> NetworkResult.Error(ApiErrorType.EmptyBody, "Empty response", response.code())
                !body.isSuccess -> NetworkResult.Error(
                    mapHttpCodeToErrorType(response.code()).takeIf { it != ApiErrorType.Unknown } ?: ApiErrorType.BadRequest,
                    body.errorMessage,
                    response.code()
                )
                body.data == null -> NetworkResult.Error(ApiErrorType.EmptyBody, body.errorMessage, response.code())
                else -> NetworkResult.Success(body.data)
            }
        } else {
            val message = parseErrorMessage(json, response.errorBody()?.string())
            NetworkResult.Error(mapHttpCodeToErrorType(response.code()), message, response.code())
        }
    } catch (io: IOException) {
        NetworkResult.Error(ApiErrorType.Offline, "No internet connection")
    } catch (e: Exception) {
        NetworkResult.Error(ApiErrorType.Unknown, e.message)
    }
}

private fun parseErrorMessage(json: Json, rawBody: String?): String? {
    if (rawBody.isNullOrBlank()) return null
    return runCatching {
        json.decodeFromString(ApiEnvelope.serializer(kotlinx.serialization.json.JsonElement.serializer()), rawBody).errorMessage
    }.getOrNull()
}
