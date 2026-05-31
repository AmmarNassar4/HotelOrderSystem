package com.ibaapps.HotelOrderSystem.data.remote

import com.ibaapps.HotelOrderSystem.data.remote.dto.ApiEnvelope
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import retrofit2.Response
import java.io.IOException

class NetworkResultTest {

    private val json = Json { ignoreUnknownKeys = true; explicitNulls = false }

    @Test
    fun success_returnsData() = runTest {
        val result = safeApiCall(json) {
            Response.success(ApiEnvelope(isSuccess = true, data = "ok"))
        }
        assertTrue(result is NetworkResult.Success)
        assertEquals("ok", (result as NetworkResult.Success).data)
    }

    @Test
    fun successStatus_butEnvelopeFailure_isBadRequest() = runTest {
        val result = safeApiCall(json) {
            Response.success(ApiEnvelope<String>(isSuccess = false, errorMessage = "bad input"))
        }
        result as NetworkResult.Error
        assertEquals(ApiErrorType.BadRequest, result.type)
        assertEquals("bad input", result.message)
    }

    @Test
    fun http401_mapsToUnauthorized() = runTest {
        val body = """{"isSuccess":false,"data":null,"errorMessage":"Invalid token."}"""
            .toResponseBody("application/json".toMediaType())
        val result = safeApiCall(json) { Response.error<ApiEnvelope<String>>(401, body) }
        result as NetworkResult.Error
        assertEquals(ApiErrorType.Unauthorized, result.type)
        assertEquals("Invalid token.", result.message)
        assertEquals(401, result.httpCode)
    }

    @Test
    fun http409_mapsToConflict() = runTest {
        val body = """{"isSuccess":false,"errorMessage":"Already accepted."}"""
            .toResponseBody("application/json".toMediaType())
        val result = safeApiCall(json) { Response.error<ApiEnvelope<String>>(409, body) }
        result as NetworkResult.Error
        assertEquals(ApiErrorType.Conflict, result.type)
        assertEquals("Already accepted.", result.message)
    }

    @Test
    fun http500_mapsToServer() = runTest {
        val body = "{}".toResponseBody("application/json".toMediaType())
        val result = safeApiCall(json) { Response.error<ApiEnvelope<String>>(500, body) }
        result as NetworkResult.Error
        assertEquals(ApiErrorType.Server, result.type)
    }

    @Test
    fun ioException_mapsToOffline() = runTest {
        val result = safeApiCall<String>(json) { throw IOException("no network") }
        result as NetworkResult.Error
        assertEquals(ApiErrorType.Offline, result.type)
    }

    @Test
    fun codeMapping_coversKnownCodes() {
        assertEquals(ApiErrorType.Unauthorized, mapHttpCodeToErrorType(401))
        assertEquals(ApiErrorType.Forbidden, mapHttpCodeToErrorType(403))
        assertEquals(ApiErrorType.NotFound, mapHttpCodeToErrorType(404))
        assertEquals(ApiErrorType.Conflict, mapHttpCodeToErrorType(409))
        assertEquals(ApiErrorType.BadRequest, mapHttpCodeToErrorType(400))
        assertEquals(ApiErrorType.Server, mapHttpCodeToErrorType(503))
        assertEquals(ApiErrorType.Unknown, mapHttpCodeToErrorType(418))
    }
}
