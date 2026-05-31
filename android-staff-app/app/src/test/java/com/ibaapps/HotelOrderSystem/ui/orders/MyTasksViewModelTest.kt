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
import org.junit.Before
import org.junit.Test
import java.time.Instant

@OptIn(ExperimentalCoroutinesApi::class)
class MyTasksViewModelTest {

    private fun order(id: Int) = Order(
        orderId = id, roomId = 1, roomNumber = "10$id", assignedTeamId = 3, assignedTeamName = "HK",
        source = "Guest", status = OrderStatus.Accepted, statusRaw = "Accepted",
        createdByUserName = null, acceptedByUserId = 7, acceptedByUserName = "me",
        createdAt = Instant.now(), acceptedAt = Instant.now(), completedAt = null, slaDueAt = null, escalatedAt = null,
        rowVersion = "rv$id", details = emptyList()
    )

    private class FakeOrderRepository : OrderRepository {
        var active: List<Order> = emptyList()
        var completeResult: NetworkResult<Order> = NetworkResult.Error(ApiErrorType.Unknown)
        var lastNotes: String? = "unset"
        override suspend fun getPending() = NetworkResult.Success(emptyList<Order>())
        override suspend fun getMyActive(): NetworkResult<List<Order>> = NetworkResult.Success(active)
        override suspend fun getOrder(orderId: Int) = completeResult
        override suspend fun accept(orderId: Int, rowVersion: String?) = completeResult
        override suspend fun complete(orderId: Int, notes: String?): NetworkResult<Order> {
            lastNotes = notes
            return completeResult
        }
        override suspend fun cancel(orderId: Int, reason: String?) = completeResult
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
    fun init_loadsActiveOrders() = runTest {
        repo.active = listOf(order(1), order(2))
        val vm = MyTasksViewModel(repo)
        assertEquals(2, vm.state.value.orders.size)
    }

    @Test
    fun complete_success_removesOrderAndPassesNote() = runTest {
        repo.active = listOf(order(1))
        val vm = MyTasksViewModel(repo)
        repo.completeResult = NetworkResult.Success(order(1).copy(status = OrderStatus.Completed))

        vm.complete(1, "done")

        assertFalse(vm.state.value.orders.any { it.orderId == 1 })
        assertEquals("done", repo.lastNotes)
        assertEquals("Order completed", vm.state.value.transientMessage)
    }

    @Test
    fun complete_blankNote_sentAsNull() = runTest {
        repo.active = listOf(order(1))
        val vm = MyTasksViewModel(repo)
        repo.completeResult = NetworkResult.Success(order(1))
        vm.complete(1, "   ")
        assertEquals(null, repo.lastNotes)
    }
}
