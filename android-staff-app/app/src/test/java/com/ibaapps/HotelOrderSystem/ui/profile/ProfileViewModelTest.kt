package com.ibaapps.HotelOrderSystem.ui.profile

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.AvailabilityStatus
import com.ibaapps.HotelOrderSystem.domain.model.HeartbeatStatus
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import com.ibaapps.HotelOrderSystem.domain.repository.AuthRepository
import com.ibaapps.HotelOrderSystem.domain.repository.PresenceRepository
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import com.ibaapps.HotelOrderSystem.util.FakeRealtimeService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@OptIn(ExperimentalCoroutinesApi::class)
@RunWith(RobolectricTestRunner::class)
class ProfileViewModelTest {

    private class FakeAuthRepository : AuthRepository {
        var logoutCalled = false
        override suspend fun login(userName: String, password: String) =
            NetworkResult.Error(com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType.Unknown)
        override suspend fun logout(): NetworkResult<Unit> { logoutCalled = true; return NetworkResult.Success(Unit) }
    }

    private class FakePresenceRepository : PresenceRepository {
        override suspend fun heartbeat(appState: String, currentScreen: String?) =
            NetworkResult.Success(HeartbeatStatus(true, false, 0, null))
        override suspend fun setAvailability(isReady: Boolean) =
            NetworkResult.Success(AvailabilityStatus(isReady, null, null))
    }

    private lateinit var session: SessionManager
    private lateinit var auth: FakeAuthRepository
    private lateinit var realtime: FakeRealtimeService
    private lateinit var presence: PresenceManager

    @Before
    fun setUp() {
        Dispatchers.setMain(UnconfinedTestDispatcher())
        val context = ApplicationProvider.getApplicationContext<Context>()
        val prefs = context.getSharedPreferences("profile_vm_test", Context.MODE_PRIVATE)
        prefs.edit().clear().commit()
        session = SessionManager(prefs).apply {
            saveSession("jwt", 0L, UserProfile(1, "HK One", "hk", "Staff", 3, "Housekeeping"))
        }
        auth = FakeAuthRepository()
        realtime = FakeRealtimeService().apply { start() }
        presence = PresenceManager(FakePresenceRepository(), session)
    }

    @After
    fun tearDown() = Dispatchers.resetMain()

    @Test
    fun logout_callsBackend_clearsSession_stopsRealtime_setsLoggedOut() = runTest {
        val vm = ProfileViewModel(auth, session, presence, realtime)

        vm.logout()

        assertTrue(auth.logoutCalled)
        assertNull(session.authToken)
        assertNull(session.profile)
        assertFalse(realtime.started)
        assertTrue(vm.state.value.loggedOut)
    }

    @Test
    fun profile_exposedFromSession() {
        val vm = ProfileViewModel(auth, session, presence, realtime)
        assertTrue(vm.state.value.profile?.userName == "hk")
    }
}
