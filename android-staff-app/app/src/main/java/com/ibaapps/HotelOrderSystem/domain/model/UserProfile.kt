package com.ibaapps.HotelOrderSystem.domain.model

import kotlinx.serialization.Serializable

/**
 * Authenticated staff user, mirrors the backend `UserProfileDto`.
 * Serializable so it can be persisted in the session store.
 */
@Serializable
data class UserProfile(
    val userId: Int,
    val fullName: String,
    val userName: String,
    val role: String,
    val teamId: Int? = null,
    val teamName: String? = null
)
