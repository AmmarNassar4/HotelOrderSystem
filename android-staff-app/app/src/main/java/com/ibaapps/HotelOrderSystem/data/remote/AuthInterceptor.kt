package com.ibaapps.HotelOrderSystem.data.remote

import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject

/** Attaches the stored JWT as a Bearer token to every outgoing request. */
class AuthInterceptor @Inject constructor(
    private val session: SessionManager
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val token = session.authToken
        val authorized = if (!token.isNullOrBlank() && request.header("Authorization") == null) {
            request.newBuilder()
                .header("Authorization", "Bearer $token")
                .build()
        } else {
            request
        }
        return chain.proceed(authorized)
    }
}
