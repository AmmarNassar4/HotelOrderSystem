package com.ibaapps.HotelOrderSystem.data.remote

import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject

/**
 * Detects 401 responses on authenticated calls and signals a forced logout.
 * The login endpoint is excluded — a 401 there just means bad credentials.
 */
class UnauthorizedInterceptor @Inject constructor(
    private val authEventBus: AuthEventBus
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val response = chain.proceed(request)
        if (response.code == 401 && !request.url.encodedPath.contains("/auth/login")) {
            authEventBus.notifyUnauthorized()
        }
        return response
    }
}
