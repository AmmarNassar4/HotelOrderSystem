package com.ibaapps.HotelOrderSystem.ui.orders

import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.OrderStatus
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
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
import java.time.Instant

@OptIn(ExperimentalCoroutinesApi::class)
class PendingViewModelTest {

    private fun order(id: Int) = Order(
        orderId = id, roomId = 1, roomNumber = "10$id", assignedTeamId = 3, assignedTeamName = "HK",
        source = "Guest", status = OrderStatus.Pending, statusRaw = "Pending",
        createdByUserName = null, acceptedByUserId = null, acceptedByUserName = null,
        createdAt = Instant.now(), acceptedAt = null, completedAt = null, slaDueAt = null, escalatedAt = null,
        rowVersion = "rv$id", details = emptyList()
    )

    private class FakeOrderRepository : OrderRepository {
        var pending: List<Order> = emptyList()
        var acceptResult: NetworkResult<Order> = NetworkResult.Error(ApiErrorType.Unknown)
        var pendingCalls = 0
        override suspend fun getPending(): NetworkResult<List<Order>> {
            pendingCalls++
            return NetworkResult.Success(pending)
        }
        override suspend fun getMyActive() = NetworkResult.Success(emptyList<Order>())
        override suspend fun getOrder(orderId: Int) = acceptResult
        override suspend fun accept(orderId: Int, rowVersion: String?) = acceptResult
        override suspend fun complete(orderId: Int, notes: String?) = acceptResult
        override suspend fun cancel(orderId: Int, reason: String?) = acceptResult
    }

    private lateinit var repo: FakeOrderRepository

    @Before
    fun setUp() {
        Dispatchers.setMain(UnconfinedTestDispatcher())
        repo = FakeOrderRepository()
    }

    @After
    fun tearDown() = Dispatchers.resetMain()

    @Test
    fun init_loadsPendingOrders() = runTest {
        repo.pending = listOf(order(1), order(2))
        val vm = PendingViewModel(repo)
        assertEquals(2, vm.state.value.orders.size)
        assertTrue(vm.state.value.loadedOnce)
    }

    @Test
    fun accept_success_removesOrderFromList() = runTest {
        repo.pending = listOf(order(1), order(2))
        val vm = PendingViewModel(repo)
        repo.acceptResult = NetworkResult.Success(order(1).copy(status = OrderStatus.Accepted))

        vm.accept(order(1))

        val state = vm.state.value
        assertFalse(state.orders.any { it.orderId == 1 })
        assertEquals(1, state.orders.size)
        assertEquals("Order accepted", state.transientMessage)
    }

    @Test
    fun accept_conflict_showsMessageAndRefreshes() = runTest {
        repo.pending = listOf(order(1), order(2))
        val vm = PendingViewModel(repo)
        val callsAfterInit = repo.pendingCalls
        repo.acceptResult = NetworkResult.Error(ApiErrorType.Conflict, "taken")

        vm.accept(order(1))

        assertTrue(vm.state.value.transientMessage!!.contains("already accepted"))
        assertTrue(repo.pendingCalls > callsAfterInit) // refreshed after conflict
        assertFalse(vm.state.value.acceptingIds.contains(1))
    }
}
