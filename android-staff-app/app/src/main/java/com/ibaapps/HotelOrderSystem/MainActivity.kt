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
        val orderId = intent?.getIntExtra(HotelFirebaseMessagingService.EXTRA_ORDER_ID, -1) ?: -1
        if (orderId > 0) deepLinkOrderId = orderId
    }
}
