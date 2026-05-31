package com.ibaapps.HotelOrderSystem.data.remote.mapper

import com.ibaapps.HotelOrderSystem.data.remote.dto.OrderDetailDto
import com.ibaapps.HotelOrderSystem.data.remote.dto.OrderDto
import com.ibaapps.HotelOrderSystem.domain.model.OrderStatus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class OrderMapperTest {

    private fun dto(
        status: String = "Pending",
        created: String = "2026-05-31T12:00:00Z",
        details: List<OrderDetailDto> = emptyList()
    ) = OrderDto(
        orderId = 1,
        roomId = 10,
        roomNumber = "101",
        assignedTeamId = 3,
        assignedTeamName = "Housekeeping",
        source = "Guest",
        status = status,
        createdAtUtc = created,
        rowVersion = "AAAA",
        details = details
    )

    @Test
    fun statusMapping_isCaseInsensitiveAndHandlesInProgress() {
        assertEquals(OrderStatus.Pending, dto(status = "Pending").toDomain().status)
        assertEquals(OrderStatus.Accepted, dto(status = "accepted").toDomain().status)
        assertEquals(OrderStatus.InProgress, dto(status = "InProgress").toDomain().status)
        assertEquals(OrderStatus.Completed, dto(status = "COMPLETED").toDomain().status)
        assertEquals(OrderStatus.Cancelled, dto(status = "Cancelled").toDomain().status)
        assertEquals(OrderStatus.Unknown, dto(status = "weird").toDomain().status)
        assertEquals("weird", dto(status = "weird").toDomain().statusRaw)
    }

    @Test
    fun parsesUtcDates_withZuluAndWithoutOffset() {
        val expectedEpoch = java.time.Instant.parse("2026-05-31T12:00:00Z").toEpochMilli()
        val withZ = dto(created = "2026-05-31T12:00:00Z").toDomain()
        val withoutZone = dto(created = "2026-05-31T12:00:00.1234567").toDomain()
        assertEquals(expectedEpoch, withZ.createdAt?.toEpochMilli())
        assertTrue(withoutZone.createdAt != null)
    }

    @Test
    fun invalidDate_becomesNull() {
        assertNull(dto(created = "not-a-date").toDomain().createdAt)
    }

    @Test
    fun parsesDynamicAttributes_intoStringMap() {
        val detail = OrderDetailDto(
            orderDetailId = 5,
            itemId = 9,
            itemName = "Towels",
            quantity = 2,
            dynamicAttributes = """{"color":"white","floor":3,"urgent":true}"""
        )
        val line = detail.toDomain()
        assertEquals(2, line.quantity)
        assertEquals("white", line.dynamicAttributes["color"])
        assertEquals("3", line.dynamicAttributes["floor"])
        assertEquals("true", line.dynamicAttributes["urgent"])
    }

    @Test
    fun emptyOrBlankDynamicAttributes_yieldsEmptyMap() {
        assertTrue(parseDynamicAttributes("{}").isEmpty())
        assertTrue(parseDynamicAttributes("").isEmpty())
        assertTrue(parseDynamicAttributes(null).isEmpty())
    }

    @Test
    fun primaryItemName_summarisesMultipleLines() {
        val one = dto(details = listOf(OrderDetailDto(1, 1, "Towels", 1))).toDomain()
        val many = dto(
            details = listOf(
                OrderDetailDto(1, 1, "Towels", 1),
                OrderDetailDto(2, 2, "Soap", 1)
            )
        ).toDomain()
        assertEquals("Towels", one.primaryItemName)
        assertEquals("Towels +1", many.primaryItemName)
    }
}
