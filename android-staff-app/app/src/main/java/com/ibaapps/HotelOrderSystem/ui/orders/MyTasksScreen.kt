package com.ibaapps.HotelOrderSystem.ui.orders

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.ui.common.toClockTimeOrDash

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyTasksScreen(
    onOpenOrderDetails: (Int) -> Unit,
    viewModel: MyTasksViewModel = hiltViewModel()
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var orderToComplete by remember { mutableStateOf<Order?>(null) }

    // Refresh active tasks on resume so REST stays authoritative (§13).
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }

    LaunchedEffect(state.transientMessage) {
        state.transientMessage?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.consumeTransientMessage()
        }
    }

    Box(Modifier.fillMaxSize()) {
        PullToRefreshBox(
            isRefreshing = state.isRefreshing,
            onRefresh = { viewModel.refresh() },
            modifier = Modifier.fillMaxSize()
        ) {
            when {
                state.isLoading -> CenterMessageBox { CircularProgressIndicator() }

                state.errorMessage != null && state.orders.isEmpty() ->
                    CenterMessageBox { CenteredText(state.errorMessage!!) }

                state.orders.isEmpty() ->
                    CenterMessageBox { CenteredText("No active tasks yet.\nAccept a pending order to get started.") }

                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    state.lastUpdatedAt?.let { updated ->
                        item {
                            Text(
                                text = "Last updated ${updated.toClockTimeOrDash()}",
                                style = MaterialTheme.typography.bodyMedium,
                                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                            )
                        }
                    }
                    items(state.orders, key = { it.orderId }) { order ->
                        OrderCard(order = order, onClick = { onOpenOrderDetails(order.orderId) }) {
                            OutlinedButton(onClick = { onOpenOrderDetails(order.orderId) }) { Text("Details") }
                            Button(
                                onClick = { orderToComplete = order },
                                enabled = order.orderId !in state.completingIds && state.isOnline,
                                modifier = Modifier.padding(start = 8.dp)
                            ) { Text("Complete") }
                        }
                    }
                }
            }
        }

        SnackbarHost(hostState = snackbarHostState, modifier = Modifier.align(Alignment.BottomCenter))
    }

    orderToComplete?.let { order ->
        CompleteDialog(
            order = order,
            onDismiss = { orderToComplete = null },
            onConfirm = { note ->
                viewModel.complete(order.orderId, note)
                orderToComplete = null
            }
        )
    }
}

@Composable
private fun CompleteDialog(
    order: Order,
    onDismiss: () -> Unit,
    onConfirm: (String) -> Unit
) {
    var note by remember { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Complete order #${order.orderId}?") },
        text = {
            androidx.compose.foundation.layout.Column {
                Text("Room ${order.roomNumber} • ${order.primaryItemName}")
                OutlinedTextField(
                    value = note,
                    onValueChange = { note = it },
                    label = { Text("Note (optional)") },
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 12.dp)
                )
            }
        },
        confirmButton = { TextButton(onClick = { onConfirm(note) }) { Text("Complete") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } }
    )
}

@Composable
private fun CenterMessageBox(content: @Composable () -> Unit) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        contentAlignment = Alignment.Center
    ) { content() }
}

@Composable
private fun CenteredText(text: String) {
    Text(text = text, style = MaterialTheme.typography.bodyLarge, textAlign = TextAlign.Center)
}
