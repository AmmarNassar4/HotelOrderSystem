package com.ibaapps.HotelOrderSystem.data.local

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class SessionManagerTest {

    private lateinit var session: SessionManager

    private val profile = UserProfile(
        userId = 7,
        fullName = "Housekeeping One",
        userName = "housekeeping",
        role = "Staff",
        teamId = 3,
        teamName = "Housekeeping"
    )

    @Before
    fun setUp() {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val prefs = context.getSharedPreferences("test_session", Context.MODE_PRIVATE)
        prefs.edit().clear().commit()
        session = SessionManager(prefs)
    }

    @Test
    fun saveSession_roundTripsTokenAndProfile() {
        val expiry = 10_000L
        session.saveSession("jwt-abc", expiry, profile)

        assertEquals("jwt-abc", session.authToken)
        assertEquals(expiry, session.tokenExpiresAtEpochMs)
        assertEquals(profile, session.profile)
    }

    @Test
    fun deviceId_isStableAcrossReads() {
        val first = session.deviceId
        val second = session.deviceId
        assertTrue(first.startsWith("android-"))
        assertEquals(first, second)
    }

    @Test
    fun isTokenValid_respectsExpiry() {
        session.saveSession("jwt-abc", expiresAtEpochMs = 5_000L, profile = profile)
        assertTrue(session.isTokenValid(nowMs = 4_999L))
        assertFalse(session.isTokenValid(nowMs = 5_001L))
    }

    @Test
    fun isTokenValid_zeroExpiryMeansValidWhenTokenPresent() {
        session.authToken = "jwt-abc"
        session.tokenExpiresAtEpochMs = 0L
        assertTrue(session.isTokenValid(nowMs = 9_999_999L))
    }

    @Test
    fun isTokenValid_falseWhenNoToken() {
        assertFalse(session.isTokenValid(nowMs = 1L))
    }

    @Test
    fun clear_wipesSessionButKeepsDeviceId() {
        val deviceId = session.deviceId
        session.saveSession("jwt-abc", 5_000L, profile)
        session.isReady = true

        session.clear()

        assertNull(session.authToken)
        assertNull(session.profile)
        assertFalse(session.isReady)
        assertEquals(0L, session.tokenExpiresAtEpochMs)
        assertEquals(deviceId, session.deviceId)
    }
}
