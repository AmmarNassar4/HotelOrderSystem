package com.ibaapps.HotelOrderSystem.data.remote

/** Transforms the payload of a [NetworkResult.Success], passing errors through unchanged. */
inline fun <T, R> NetworkResult<T>.map(transform: (T) -> R): NetworkResult<R> = when (this) {
    is NetworkResult.Success -> NetworkResult.Success(transform(data))
    is NetworkResult.Error -> this
}
