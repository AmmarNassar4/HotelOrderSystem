package com.ibaapps.HotelOrderSystem.data.remote

import com.ibaapps.HotelOrderSystem.data.remote.dto.AcceptOrderRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.AckDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.ApiEnvelope
import com.ibaapps.HotelOrderSystem.data.remote.dto.AuthResponseDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.AvailabilityRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.AvailabilityResponseDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.CancelOrderRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.CompleteOrderRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.DeviceTokenRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.HeartbeatRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.HeartbeatResponseDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.LoginRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.LogoutRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.OrderDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PUT
import retrofit2.http.POST
import retrofit2.http.Path

interface StaffApiService {

    @POST("api/v1/auth/login")
    suspend fun login(@Body body: LoginRequestDto): Response<ApiEnvelope<AuthResponseDto>>

    @PUT("api/v1/auth/device-token")
    suspend fun registerDevice(@Body body: DeviceTokenRequestDto): Response<ApiEnvelope<AckDto>>

    @POST("api/v1/auth/logout")
    suspend fun logout(@Body body: LogoutRequestDto): Response<ApiEnvelope<AckDto>>

    @GET("api/v1/orders/pending")
    suspend fun getPending(): Response<ApiEnvelope<List<OrderDto>>>

    @GET("api/v1/orders/my-active")
    suspend fun getMyActive(): Response<ApiEnvelope<List<OrderDto>>>

    @GET("api/v1/orders/{id}")
    suspend fun getOrder(@Path("id") id: Int): Response<ApiEnvelope<OrderDto>>

    @PUT("api/v1/orders/{id}/accept")
    suspend fun acceptOrder(
        @Path("id") id: Int,
        @Body body: AcceptOrderRequestDto
    ): Response<ApiEnvelope<OrderDto>>

    @PUT("api/v1/orders/{id}/complete")
    suspend fun completeOrder(
        @Path("id") id: Int,
        @Body body: CompleteOrderRequestDto
    ): Response<ApiEnvelope<OrderDto>>

    @PUT("api/v1/orders/{id}/cancel")
    suspend fun cancelOrder(
        @Path("id") id: Int,
        @Body body: CancelOrderRequestDto
    ): Response<ApiEnvelope<OrderDto>>

    @PUT("api/v1/presence/heartbeat")
    suspend fun heartbeat(@Body body: HeartbeatRequestDto): Response<ApiEnvelope<HeartbeatResponseDto>>

    @PUT("api/v1/presence/availability")
    suspend fun setAvailability(@Body body: AvailabilityRequestDto): Response<ApiEnvelope<AvailabilityResponseDto>>
}
