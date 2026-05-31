package com.ibaapps.HotelOrderSystem.ui.status

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.AvailabilityStatus
import com.ibaapps.HotelOrderSystem.domain.model.HeartbeatStatus
import com.ibaapps.HotelOrderSystem.domain.repository.PresenceRepository
import com.ibaapps.HotelOrderSystem.presence.PresenceManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@OptIn(ExperimentalCoroutinesApi::class)
@RunWith(RobolectricTestRunner::class)
class StatusViewModelTest {

    private class FakePresenceRepository : PresenceRepository {
        var availabilityResult: NetworkResult<AvailabilityStatus> =
            NetworkResult.Success(AvailabilityStatus(isReady = true, readySince = null, changedAt = null))
        override suspend fun heartbeat(appState: String, currentScreen: String?): NetworkResult<HeartbeatStatus> =
            NetworkResult.Success(HeartbeatStatus(online = true, isReady = false, pendingOrdersCount = 0, serverTime = null))
        override suspend fun setAvailability(isReady: Boolean) = availabilityResult
    }

    private lateinit var repo: FakePresenceRepository
    private lateinit var presenceManager: PresenceManager
    private lateinit var session: SessionManager
    private lateinit var vm: StatusViewModel

    @Before
    fun setUp() {
        Dispatchers.setMain(UnconfinedTestDispatcher())
        val context = ApplicationProvider.getApplicationContext<Context>()
        val prefs = context.getSharedPreferences("status_vm_test", Context.MODE_PRIVATE)
        prefs.edit().clear().commit()
        session = SessionManager(prefs)
        repo = FakePresenceRepository()
        presenceManager = PresenceManager(repo, session)
        vm = StatusViewModel(repo, presenceManager, session)
    }

    @After
    fun tearDown() = Dispatchers.resetMain()

    @Test
    fun setReady_success_keepsReadyAndClearsToggling() = runTest {
        repo.availabilityResult = NetworkResult.Success(AvailabilityStatus(true, null, null))
        vm.setReady(true)
        val state = vm.state.first { !it.isToggling }
        assertTrue(state.isReady)
        assertFalse(state.isToggling)
    }

    @Test
    fun setReady_failure_revertsAndSurfacesError() = runTest {
        repo.availabilityResult = NetworkResult.Error(ApiErrorType.Server, "Server error")
        vm.setReady(true)
        val state = vm.state.first { it.errorMessage != null }
        assertFalse(state.isReady) // reverted from optimistic true
        assertNotNull(state.errorMessage)
    }
}
