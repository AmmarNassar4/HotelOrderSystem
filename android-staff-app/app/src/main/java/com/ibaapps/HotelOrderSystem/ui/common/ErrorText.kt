package com.ibaapps.HotelOrderSystem.ui.common

import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult

/**
 * Maps an API error to a user-facing message. Prefers the backend's own
 * errorMessage; otherwise falls back to a friendly default per error type.
 */
fun NetworkResult.Error.toUserMessage(): String =
    message?.takeIf { it.isNotBlank() } ?: when (type) {
        ApiErrorType.Unauthorized -> "Your session has expired. Please sign in again."
        ApiErrorType.Forbidden -> "You don't have access to this."
        ApiErrorType.Conflict -> "This was just changed by someone else. Please refresh."
        ApiErrorType.NotFound -> "Not found."
        ApiErrorType.Offline -> "No internet connection."
        ApiErrorType.Server -> "Server error. Please try again."
        ApiErrorType.EmptyBody -> "No data returned."
        ApiErrorType.BadRequest -> "Request could not be completed."
        ApiErrorType.Unknown -> "Something went wrong. Please try again."
    }
