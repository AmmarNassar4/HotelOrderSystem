package com.ibaapps.HotelOrderSystem.ui.status

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.core.app.NotificationManagerCompat
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.ibaapps.HotelOrderSystem.ui.common.toClockTimeOrDash
import com.ibaapps.HotelOrderSystem.ui.theme.NotReadyOrange
import com.ibaapps.HotelOrderSystem.ui.theme.OfflineRed
import com.ibaapps.HotelOrderSystem.ui.theme.ReadyGreen

@Composable
fun StatusScreen(viewModel: StatusViewModel = hiltViewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val context = LocalContext.current
    val pushEnabled = NotificationManagerCompat.from(context).areNotificationsEnabled()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text("Status & availability", style = MaterialTheme.typography.titleLarge)

        // Ready toggle
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(
                containerColor = if (state.isReady) ReadyGreen.copy(alpha = 0.12f) else NotReadyOrange.copy(alpha = 0.12f)
            )
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(20.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column(Modifier.weight(1f)) {
                    Text(
                        text = if (state.isReady) "Ready for orders" else "Not ready",
                        style = MaterialTheme.typography.titleMedium,
                        color = if (state.isReady) ReadyGreen else NotReadyOrange
                    )
                    Text(
                        text = if (state.isReady) "You will receive new team requests" else "You won't receive new requests",
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
                Switch(
                    checked = state.isReady,
                    enabled = !state.isToggling,
                    onCheckedChange = { viewModel.setReady(it) }
                )
            }
        }

        state.errorMessage?.let {
            Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodyMedium)
        }

        // Details
        Card(modifier = Modifier.fillMaxWidth()) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                StatusRow(
                    label = "Connection",
                    value = if (state.online) "Online" else "Offline",
                    dotColor = if (state.online) ReadyGreen else OfflineRed
                )
                StatusRow(label = "Last heartbeat", value = state.lastHeartbeatAt.toClockTimeOrDash())
                StatusRow(label = "Team", value = state.teamName ?: "—")
                StatusRow(label = "Role", value = state.role ?: "—")
                StatusRow(label = "Pending for team", value = state.pendingOrdersCount.toString())
                StatusRow(
                    label = "Push notifications",
                    value = if (pushEnabled) "Enabled" else "Disabled",
                    dotColor = if (pushEnabled) ReadyGreen else OfflineRed
                )
            }
        }
    }
}

@Composable
private fun StatusRow(label: String, value: String, dotColor: Color? = null) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(label, style = MaterialTheme.typography.bodyMedium)
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            if (dotColor != null) {
                Surface(modifier = Modifier.size(10.dp), shape = CircleShape, color = dotColor) {}
            }
            Text(value, style = MaterialTheme.typography.titleMedium)
        }
    }
}
