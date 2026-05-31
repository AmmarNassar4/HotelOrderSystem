package com.ibaapps.HotelOrderSystem.ui.orders

import android.content.Context
import androidx.lifecycle.SavedStateHandle
import androidx.test.core.app.ApplicationProvider
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.OrderStatus
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
import com.ibaapps.HotelOrderSystem.ui.navigation.Routes
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import java.time.Instant

@OptIn(ExperimentalCoroutinesApi::class)
@RunWith(RobolectricTestRunner::class)
class OrderDetailsViewModelTest {

    private fun order(status: OrderStatus = OrderStatus.Pending) = Order(
        orderId = 5, roomId = 1, roomNumber = "501", assignedTeamId = 3, assignedTeamName = "HK",
        source = "Guest", status = status, statusRaw = status.name,
        createdByUserName = null, acceptedByUserId = null, acceptedByUserName = null,
        createdAt = Instant.now(), acceptedAt = null, completedAt = null, slaDueAt = null, escalatedAt = null,
        rowVersion = "rv5", details = emptyList()
    )

    private class FakeOrderRepository : OrderRepository {
        var getResult: NetworkResult<Order> = NetworkResult.Error(ApiErrorType.NotFound)
        var acceptResult: NetworkResult<Order> = NetworkResult.Error(ApiErrorType.Unknown)
        var getCalls = 0
        override suspend fun getPending() = NetworkResult.Success(emptyList<Order>())
        override suspend fun getMyActive() = NetworkResult.Success(emptyList<Order>())
        override suspend fun getOrder(orderId: Int): NetworkResult<Order> { getCalls++; return getResult }
        override suspend fun accept(orderId: Int, rowVersion: String?) = acceptResult
        override suspend fun complete(orderId: Int, notes: String?) = acceptResult
        override suspend fun cancel(orderId: Int, reason: String?) = acceptResult
    }

    private lateinit var repo: FakeOrderRepository
    private lateinit var session: SessionManager

    private fun newSession(role: String): SessionManager {
        val context = ApplicationProvider.getApplicationContext<Context>()
        val prefs = context.getSharedPreferences("details_$role", Context.MODE_PRIVATE)
        prefs.edit().clear().commit()
        return SessionManager(prefs).apply {
            profile = UserProfile(1, "User", "u", role, 3, "HK")
        }
    }

    private fun vm() = OrderDetailsViewModel(repo, session, SavedStateHandle(mapOf(Routes.ARG_ORDER_ID to 5)))

    @Before
    fun setUp() {
        Dispatchers.setMain(UnconfinedTestDispatcher())
        repo = FakeOrderRepository()
    }

    @After
    fun tearDown() = Dispatchers.resetMain()

    @Test
    fun load_success_populatesOrder() = runTest {
        session = newSession("Staff")
        repo.getResult = NetworkResult.Success(order())
        val vm = vm()
        assertEquals(5, vm.state.value.order?.orderId)
        assertFalse(vm.state.value.isLoading)
    }

    @Test
    fun canCancel_trueForSupervisor_falseForStaff() = runTest {
        repo.getResult = NetworkResult.Success(order())

        session = newSession("Staff")
        assertFalse(vm().state.value.canCancel)

        session = newSession("Supervisor")
        assertTrue(vm().state.value.canCancel)
    }

    @Test
    fun accept_conflict_reloadsOrder() = runTest {
        session = newSession("Staff")
        repo.getResult = NetworkResult.Success(order())
        val vm = vm()
        val callsAfterInit = repo.getCalls
        repo.acceptResult = NetworkResult.Error(ApiErrorType.Conflict, "taken")

        vm.accept()

        assertTrue(vm.state.value.transientMessage!!.contains("already accepted"))
        assertTrue(repo.getCalls > callsAfterInit)
    }
}
