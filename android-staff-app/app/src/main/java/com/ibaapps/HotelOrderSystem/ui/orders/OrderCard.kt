package com.ibaapps.HotelOrderSystem.ui.orders

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.OrderStatus
import com.ibaapps.HotelOrderSystem.ui.common.toRelativeAgo
import com.ibaapps.HotelOrderSystem.ui.theme.StatusCancelled
import com.ibaapps.HotelOrderSystem.ui.theme.StatusCompleted
import com.ibaapps.HotelOrderSystem.ui.theme.StatusInProgress
import com.ibaapps.HotelOrderSystem.ui.theme.StatusPending
import com.ibaapps.HotelOrderSystem.ui.theme.StatusUrgent

fun statusColor(status: OrderStatus): Color = when (status) {
    OrderStatus.Pending -> StatusPending
    OrderStatus.Accepted, OrderStatus.InProgress -> StatusInProgress
    OrderStatus.Completed -> StatusCompleted
    OrderStatus.Cancelled -> StatusCancelled
    OrderStatus.Unknown -> StatusCancelled
}

/**
 * Reusable mobile-first order card (§10): prominent room number and item name,
 * secondary metadata, status + urgency tags, and a trailing action slot.
 */
@Composable
fun OrderCard(
    order: Order,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    actions: @Composable RowScope.() -> Unit = {}
) {
    val totalQty = order.details.sumOf { it.quantity }

    Card(
        onClick = onClick,
        modifier = modifier.fillMaxWidth(),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Room ${order.roomNumber}",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )
                Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    if (order.isUrgent()) Tag(text = if (order.isEscalated) "Escalated" else "Due", color = StatusUrgent)
                    Tag(text = order.statusRaw, color = statusColor(order.status))
                }
            }

            Text(
                text = order.primaryItemName,
                style = MaterialTheme.typography.titleMedium
            )

            Text(
                text = buildString {
                    append("Qty $totalQty")
                    append("  •  #${order.orderId}")
                    order.assignedTeamName?.let { append("  •  $it") }
                    append("  •  ${order.createdAt.toRelativeAgo()}")
                },
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
            )

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End,
                verticalAlignment = Alignment.CenterVertically,
                content = actions
            )
        }
    }
}

private fun Order.isUrgent(now: java.time.Instant = java.time.Instant.now()): Boolean =
    isEscalated || (slaDueAt != null && slaDueAt.isBefore(now) && status == OrderStatus.Pending)

@Composable
private fun Tag(text: String, color: Color) {
    Surface(
        shape = RoundedCornerShape(6.dp),
        color = color.copy(alpha = 0.15f)
    ) {
        Text(
            text = text,
            color = color,
            style = MaterialTheme.typography.labelLarge,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
        )
    }
}
