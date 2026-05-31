package com.ibaapps.HotelOrderSystem

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.ibaapps.HotelOrderSystem.ui.theme.HotelStaffTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            HotelStaffTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    StaffAppRoot()
                }
            }
        }
    }
}

/**
 * Root navigation host. Currently a single placeholder destination; the real
 * splash → login → main scaffold graph is built in Task 6.
 */
@Composable
private fun StaffAppRoot() {
    val navController = rememberNavController()
    NavHost(navController = navController, startDestination = "placeholder") {
        composable("placeholder") { PlaceholderScreen() }
    }
}

@Composable
private fun PlaceholderScreen() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(text = "Hotel Staff", style = MaterialTheme.typography.headlineSmall)
        Text(
            text = "Native app foundation ready",
            style = MaterialTheme.typography.bodyMedium
        )
    }
}
