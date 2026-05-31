package com.ibaapps.HotelOrderSystem

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.ibaapps.HotelOrderSystem.push.HotelFirebaseMessagingService
import com.ibaapps.HotelOrderSystem.push.parseOrderIdFromData
import com.ibaapps.HotelOrderSystem.ui.navigation.AppNavHost
import com.ibaapps.HotelOrderSystem.ui.theme.HotelStaffTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {

    private var deepLinkOrderId by mutableStateOf<Int?>(null)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        handleDeepLink(intent)
        setContent {
            HotelStaffTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    AppNavHost(
                        deepLinkOrderId = deepLinkOrderId,
                        onDeepLinkConsumed = { deepLinkOrderId = null }
                    )
                }
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleDeepLink(intent)
    }

    private fun handleDeepLink(intent: Intent?) {
        if (intent == null) return

        // From our own foreground-built notification.
        val direct = intent.getIntExtra(HotelFirebaseMessagingService.EXTRA_ORDER_ID, -1)
        if (direct > 0) {
            deepLinkOrderId = direct
            return
        }

        // From a background system-tray notification: FCM delivers the data
        // payload (type/payload) as string intent extras; orderId is nested in
        // the payload JSON.
        val data = buildMap {
            intent.getStringExtra("orderId")?.let { put("orderId", it) }
            intent.getStringExtra("payload")?.let { put("payload", it) }
        }
        parseOrderIdFromData(data)?.let { deepLinkOrderId = it }
    }
}
