package com.ibaapps.HotelOrderSystem.data.repository

import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.data.remote.StaffApiService
import com.ibaapps.HotelOrderSystem.data.remote.dto.AcceptOrderRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.CancelOrderRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.CompleteOrderRequestDto
import com.ibaapps.HotelOrderSystem.data.remote.map
import com.ibaapps.HotelOrderSystem.data.remote.mapper.toDomain
import com.ibaapps.HotelOrderSystem.data.remote.safeApiCall
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
import kotlinx.serialization.json.Json
import javax.inject.Inject

class OrderRepositoryImpl @Inject constructor(
    private val api: StaffApiService,
    private val json: Json
) : OrderRepository {

    override suspend fun getPending(): NetworkResult<List<Order>> =
        safeApiCall(json) { api.getPending() }.map { list -> list.map { it.toDomain() } }

    override suspend fun getMyActive(): NetworkResult<List<Order>> =
        safeApiCall(json) { api.getMyActive() }.map { list -> list.map { it.toDomain() } }

    override suspend fun getOrder(orderId: Int): NetworkResult<Order> =
        safeApiCall(json) { api.getOrder(orderId) }.map { it.toDomain() }

    override suspend fun accept(orderId: Int, rowVersion: String?): NetworkResult<Order> =
        safeApiCall(json) { api.acceptOrder(orderId, AcceptOrderRequestDto(rowVersion)) }.map { it.toDomain() }

    override suspend fun complete(orderId: Int, notes: String?): NetworkResult<Order> =
        safeApiCall(json) { api.completeOrder(orderId, CompleteOrderRequestDto(notes)) }.map { it.toDomain() }

    override suspend fun cancel(orderId: Int, reason: String?): NetworkResult<Order> =
        safeApiCall(json) { api.cancelOrder(orderId, CancelOrderRequestDto(reason)) }.map { it.toDomain() }
}
