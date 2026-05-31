package com.ibaapps.HotelOrderSystem.ui.orders

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.ibaapps.HotelOrderSystem.domain.model.Order
import com.ibaapps.HotelOrderSystem.domain.model.OrderLine
import com.ibaapps.HotelOrderSystem.domain.model.OrderStatus
import com.ibaapps.HotelOrderSystem.ui.common.toDateTimeOrDash

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OrderDetailsScreen(
    onBack: () -> Unit,
    viewModel: OrderDetailsViewModel = hiltViewModel()
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var showCompleteDialog by remember { mutableStateOf(false) }
    var showCancelDialog by remember { mutableStateOf(false) }

    LaunchedEffect(state.transientMessage) {
        state.transientMessage?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.consumeTransientMessage()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.order?.let { "Order #${it.orderId}" } ?: "Order") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            when {
                state.isLoading -> CircularProgressIndicator(Modifier.align(Alignment.Center))
                state.order == null -> Text(
                    text = state.errorMessage ?: "Order not found.",
                    modifier = Modifier
                        .align(Alignment.Center)
                        .padding(24.dp)
                )
                else -> OrderDetailsContent(
                    order = state.order!!,
                    canCancel = state.canCancel,
                    actionInProgress = state.actionInProgress,
                    onAccept = viewModel::accept,
                    onCompleteClick = { showCompleteDialog = true },
                    onCancelClick = { showCancelDialog = true }
                )
            }
        }
    }

    if (showCompleteDialog) {
        NoteDialog(
            title = "Complete this order?",
            label = "Note (optional)",
            confirmText = "Complete",
            onDismiss = { showCompleteDialog = false },
            onConfirm = { note -> viewModel.complete(note); showCompleteDialog = false }
        )
    }
    if (showCancelDialog) {
        NoteDialog(
            title = "Cancel this order?",
            label = "Reason (optional)",
            confirmText = "Cancel order",
            onDismiss = { showCancelDialog = false },
            onConfirm = { reason -> viewModel.cancel(reason); showCancelDialog = false }
        )
    }
}

@Composable
private fun OrderDetailsContent(
    order: Order,
    canCancel: Boolean,
    actionInProgress: Boolean,
    onAccept: () -> Unit,
    onCompleteClick: () -> Unit,
    onCancelClick: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("Room ${order.roomNumber}", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Surface(color = statusColor(order.status).copy(alpha = 0.15f)) {
                Text(
                    order.statusRaw,
                    color = statusColor(order.status),
                    style = MaterialTheme.typography.labelLarge,
                    modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
                )
            }
        }

        Card(Modifier.fillMaxWidth()) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text("Requested items", style = MaterialTheme.typography.titleMedium)
                order.details.forEach { line -> OrderLineRow(line) }
                if (order.details.isEmpty()) Text("No item lines.", style = MaterialTheme.typography.bodyMedium)
            }
        }

        Card(Modifier.fillMaxWidth()) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                InfoRow("Source", order.source)
                InfoRow("Team", order.assignedTeamName ?: "—")
                InfoRow("Created", order.createdAt.toDateTimeOrDash())
                InfoRow("Accepted by", order.acceptedByUserName ?: "—")
                InfoRow("Accepted", order.acceptedAt.toDateTimeOrDash())
                if (order.completedAt != null) InfoRow("Completed", order.completedAt.toDateTimeOrDash())
            }
        }

        ActionButtons(
            order = order,
            canCancel = canCancel,
            actionInProgress = actionInProgress,
            onAccept = onAccept,
            onCompleteClick = onCompleteClick,
            onCancelClick = onCancelClick
        )
    }
}

@Composable
private fun ActionButtons(
    order: Order,
    canCancel: Boolean,
    actionInProgress: Boolean,
    onAccept: () -> Unit,
    onCompleteClick: () -> Unit,
    onCancelClick: () -> Unit
) {
    val active = order.status == OrderStatus.Accepted || order.status == OrderStatus.InProgress
    Column(verticalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
        if (order.status == OrderStatus.Pending) {
            Button(onClick = onAccept, enabled = !actionInProgress, modifier = Modifier.fillMaxWidth()) {
                Text("Accept")
            }
        }
        if (active) {
            Button(onClick = onCompleteClick, enabled = !actionInProgress, modifier = Modifier.fillMaxWidth()) {
                Text("Complete")
            }
        }
        if (canCancel && (order.status == OrderStatus.Pending || active)) {
            OutlinedButton(onClick = onCancelClick, enabled = !actionInProgress, modifier = Modifier.fillMaxWidth()) {
                Text("Cancel order")
            }
        }
    }
}

@Composable
private fun OrderLineRow(line: OrderLine) {
    Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(line.itemName, style = MaterialTheme.typography.bodyLarge, fontWeight = FontWeight.SemiBold)
            Text("× ${line.quantity}", style = MaterialTheme.typography.bodyLarge)
        }
        line.dynamicAttributes.forEach { (key, value) ->
            Text(
                text = "$key: $value",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
            )
        }
    }
}

@Composable
private fun InfoRow(label: String, value: String) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(label, style = MaterialTheme.typography.bodyMedium)
        Text(value, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold)
    }
}

@Composable
private fun NoteDialog(
    title: String,
    label: String,
    confirmText: String,
    onDismiss: () -> Unit,
    onConfirm: (String) -> Unit
) {
    var text by remember { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            Column {
                OutlinedTextField(
                    value = text,
                    onValueChange = { text = it },
                    label = { Text(label) },
                    modifier = Modifier.fillMaxWidth()
                )
                Spacer(Modifier.padding(4.dp))
            }
        },
        confirmButton = { TextButton(onClick = { onConfirm(text) }) { Text(confirmText) } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Back") } }
    )
}
