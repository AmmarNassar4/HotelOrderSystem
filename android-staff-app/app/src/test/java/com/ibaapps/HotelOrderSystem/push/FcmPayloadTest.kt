package com.ibaapps.HotelOrderSystem.push

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class FcmPayloadTest {

    @Test
    fun parsesDirectOrderIdField() {
        assertEquals(123, parseOrderIdFromData(mapOf("orderId" to "123")))
    }

    @Test
    fun parsesOrderIdFromNestedPayloadJson() {
        val data = mapOf("type" to "OrderClaimed", "payload" to """{"orderId":456,"acceptedByUserId":88}""")
        assertEquals(456, parseOrderIdFromData(data))
    }

    @Test
    fun returnsNullWhenAbsentOrUnparseable() {
        assertNull(parseOrderIdFromData(emptyMap()))
        assertNull(parseOrderIdFromData(mapOf("orderId" to "abc")))
        assertNull(parseOrderIdFromData(mapOf("payload" to "not-json")))
    }

    @Test
    fun titleMapsBackendUpperSnakeTypes() {
        // Actual backend NotificationTypes are UPPER_SNAKE.
        assertEquals("New order", notificationTitleFor("ORDER_CREATED"))
        assertEquals("Order claimed", notificationTitleFor("ORDER_CLAIMED"))
        assertEquals("Order claimed", notificationTitleFor("ORDER_ACCEPTED"))
        assertEquals("Order completed", notificationTitleFor("ORDER_COMPLETED"))
        assertEquals("Order escalated", notificationTitleFor("SLA_ESCALATED"))
        assertEquals("Hotel order update", notificationTitleFor(null))
    }

    @Test
    fun titleAlsoAcceptsPascalCaseDefensively() {
        assertEquals("New order", notificationTitleFor("OrderCreated"))
        assertEquals("Order completed", notificationTitleFor("OrderCompleted"))
    }
}
